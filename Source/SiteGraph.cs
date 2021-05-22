using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Apogee.Bot
{
    internal class SiteGraph : IEnumerable<PageNode>
    {
        private ConcurrentDictionary<Uri, PageNode?> dictionary = new ConcurrentDictionary<Uri, PageNode?>();

        public bool TryAdd(Uri uri) => dictionary.TryAdd(uri, null);

        public bool TrySet(Uri uri, HttpMethod method, Status status, HttpStatusCode statusCode, string error = "")
            => TrySet(new PageNode(uri, method, status, statusCode, error));

        public bool TrySet(PageNode node) => dictionary.TryUpdate(node.Uri, node, null);

        public bool Contains(Uri uri) => dictionary.ContainsKey(uri);

        public PageNode? Get(string uriString) => Get(new Uri(uriString));

        public PageNode? Get(Uri uri) => dictionary.GetValueOrDefault(uri);

        public void Display()
        {
            var nodes = from item in this
                        orderby item.Status
                        select item;

            var empty = from item in dictionary
                        where item.Value is null
                        select item.Key;

            foreach (var node in nodes)
            {
                var uri = node.Uri.AbsoluteUri;
                var uriString = uri.Length <= 120 ? uri : uri.Substring(0, 120);
                Console.Write($"{node.Method,8} : {node.Status,8} : {uriString}");
                if (node.Status != Status.Success)
                    Console.Write($" | {node.Error}");
                Console.WriteLine();
            }

            foreach (var item in empty)
                Console.WriteLine($"{"<Node is null>",19} : {item}");
        }

        public void DisplayStats()
        {
            const int alignment = 4;
            Console.WriteLine($"{this.Count(),alignment} item(s) total.");
            Console.WriteLine($"{this.Count(i => i.Status == Status.Success),alignment} item(s) succeeded.");
            Console.WriteLine($"{this.Count(i => i.Status == Status.Error),alignment} item(s) errorred out.");
            Console.WriteLine($"{this.Count(i => i.Status == Status.Canceled),alignment} item(s) were canceled.");
            Console.WriteLine($"{this.Count(i => i.Status == Status.Failure),alignment} item(s) failed.");
        }

        public IEnumerator<PageNode> GetEnumerator()
        {
            foreach (var item in dictionary.Values)
                if (item != null)
                    yield return item;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    enum Status
    {
        Success,
        Error,
        Canceled,
        Failure
    }

    internal class PageNode
    {
        public PageNode(Uri uri, HttpMethod method, Status status, HttpStatusCode statusCode, string error)
        {
            Uri = uri;
            Method = method;
            Status = status;
            StatusCode = statusCode;
            Error = error;
        }

        public Uri Uri { get; }
        public HttpMethod Method { get; set; }
        public Status Status { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Error { get; set; }
    }
}
