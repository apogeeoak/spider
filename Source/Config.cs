using System;
using System.Linq;
using System.Text;

namespace Apogee.Bot
{
    internal class Config
    {
        public Uri Uri { get; }

        public Config(Uri uri)
        {
            Uri = uri;
        }

        public static string TryCreate(string[] args, out Config? config)
        {
            string uriString = args.DefaultIfEmpty(string.Empty).First();

            if (string.IsNullOrWhiteSpace(uriString))
            {
                config = null;
                return "spider: URI argument missing";
            }

            if (!System.Uri.TryCreate(uriString, UriKind.Absolute, out Uri? uri))
            {
                config = null;
                return $"spider: URI argument invalid ({uriString})\nThe URI must be a well formed absolute identifier.";
            }

            config = new(uri);
            return string.Empty;
        }

        public static string Usage()
        {
            StringBuilder builder = new();
            builder.AppendLine("Usage: spider URI");
            builder.AppendLine("Crawl website starting at URI.");
            return builder.ToString();
        }
    }
}
