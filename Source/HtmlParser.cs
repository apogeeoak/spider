using System.Linq;
using System.Text.RegularExpressions;

namespace Apogee.Bot
{
    internal class HtmlParser
    {
        // <a href="($1)"> : <link href="($1)"> : <script src="($1)">
        // <a [^>]*href=\"([^\"]*) : <link [^>]*href=\"([^\"]*) : <script [^>]*src=\"([^\"]*)
        private static readonly Regex linksRegex = new Regex("<(?:a|link) [^>]*href=\"(?<1>[^\"]*)|<script [^>]*src=\"(?<1>[^\"]*)");

        public string[] GetLinks(string body)
        {
            var matches = linksRegex.Matches(body);
            return matches.Select(m => m.Groups[1].Value).Distinct().ToArray();
        }
    }
}
