using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace Apogee.Bot
{
    internal class Spider
    {
        private readonly HttpClient client;
        private HtmlParser parser;

        public Spider() : this(TimeSpan.FromSeconds(3), 10, new HtmlParser()) { }
        public Spider(TimeSpan timeout, int maxConnectionsPerServer, HtmlParser parser)
        {
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = timeout,
                MaxConnectionsPerServer = maxConnectionsPerServer
            };

            this.client = new HttpClient(handler);
            this.parser = parser;
        }

        public SiteGraph Crawl(Config config) => Crawl(config.Uri);
        public SiteGraph Crawl(Uri uri) => CrawlAsync(uri).GetAwaiter().GetResult();
        public async Task<SiteGraph> CrawlAsync(Config config) => await CrawlAsync(config.Uri);

        public async Task<SiteGraph> CrawlAsync(Uri uri)
        {
            var graph = new SiteGraph();
            await RequestAsync(uri, uri, graph);
            return graph;
        }

        private async Task<Unit> RequestAsync(Uri uri, Uri origin, SiteGraph graph)
        {
            /// Return empty if not http or https.
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return Unit.Empty;

            /// Attempt to add uri to graph. Return if already added.
            if (!graph.TryAdd(uri))
                return Unit.Empty;

            /// Send HEAD request if uri is on a different host.
            if (uri.Host != origin.Host)
                return (Unit)graph.TrySet(await HeadAsync(uri));

            /// Send HEAD request for known file types.
            var extensions = new[] { ".css", ".gif", ".ico", ".jpg", ".jpeg", ".js", ".pdf", ".png", ".svg", ".xml" };
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (extensions.Contains(extension))
                return (Unit)graph.TrySet(await HeadAsync(uri));

            /// Otherwise send GET request.
            (var node, var links) = await GetAsync(uri);
            graph.TrySet(node);
            var tasks = from link in links
                        let linkUri = TryCreateUri(origin, link)
                        where linkUri is Uri
                        select RequestAsync(linkUri, origin, graph);
            await Task.WhenAll(tasks);
            return Unit.Empty;
        }

        private async Task<PageNode> HeadAsync(Uri uri) => (await TrySendAsync(uri, HttpMethod.Head)).PageNode;

        private async Task<Linked> GetAsync(Uri uri)
        {
            (var node, var response) = await TrySendAsync(uri, HttpMethod.Get);
            var links = await GetLinks(response);
            return new Linked(node, links);
        }

        /// Get links to other objects.
        private async Task<string[]> GetLinks(HttpResponseMessage? response)
        {
            if (response?.Content?.Headers.ContentType?.MediaType?.StartsWith("text") != true)
                return Array.Empty<string>();

            var body = await response.Content.ReadAsStringAsync();
            response.Dispose();
            return parser.GetLinks(body);
        }

        private Uri? TryCreateUri(Uri baseUri, string relativeUri)
        {
            if (relativeUri == "#") return null;
            relativeUri = Regex.Replace(relativeUri, "(?<!:)/{2,}", "/");

            if (!Uri.TryCreate(baseUri, relativeUri, out var uri))
                Console.WriteLine($"Error: Unable to create uri from {relativeUri}");
            return uri;
        }

        private async Task<Response> TrySendAsync(Uri uri, HttpMethod method)
        {
            try
            {
                var request = new HttpRequestMessage(method, uri);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Uri: {uri} | {method} request succeeded");
                    return new Response(uri, method, Status.Success, response.StatusCode, error: string.Empty, response);
                }

                var error = $"Error obtaining response. Response code: {(int)response.StatusCode} {response.StatusCode}";
                Console.WriteLine($"Uri: {uri} | {error}");
                return new Response(uri, method, Status.Error, response.StatusCode, error, null);
            }
            catch (TaskCanceledException)
            {
                var error = "TaskCanceledException: Request timeout.";
                Console.WriteLine($"Uri: {uri} | {error}");
                return new Response(uri, method, Status.Canceled, HttpStatusCode.RequestTimeout, error, null);
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                if (ex.InnerException != null)
                    error += $" {ex.InnerException.Message}";
                Console.WriteLine($"Uri: {uri} | {ex}");
                return new Response(uri, method, Status.Failure, HttpStatusCode.BadRequest, error, null);
            }
        }

        private struct Response
        {
            public Response(Uri uri, HttpMethod method, Status status, HttpStatusCode statusCode, string error, HttpResponseMessage? response)
            {
                PageNode = new PageNode(uri, method, status, statusCode, error);
                ResponseMessage = response;
            }

            public PageNode PageNode { get; }
            public HttpResponseMessage? ResponseMessage { get; }

            public void Deconstruct(out PageNode node, out HttpResponseMessage? response) => (node, response) = (PageNode, ResponseMessage);
        }

        private struct Linked
        {
            public Linked(PageNode node, string[] links)
            {
                PageNode = node;
                Links = links;
            }

            public PageNode PageNode { get; }
            public string[] Links { get; }

            public void Deconstruct(out PageNode node, out string[] links) => (node, links) = (PageNode, Links);
        }
    }
}
