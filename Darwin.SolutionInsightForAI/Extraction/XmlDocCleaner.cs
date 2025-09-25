using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Extraction
{
    public static class XmlDocCleaner
    {
        static readonly Regex Tags = new(@"<\/?summary>|<\/?remarks>|<\/?para>|<\/?list.*?>|<\/?item>|<\/?c>|<\/?see.*?\/?>|<\/?typeparam.*?>|<\/?inheritdoc.*?\/?>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public static string Clean(string? xmlish)
        {
            if (string.IsNullOrWhiteSpace(xmlish)) return string.Empty;

            // escape (\u003Csummary\u003E ...)
            var decoded = System.Web.HttpUtility.HtmlDecode(xmlish);

            // remove tags and space
            var noTags = Tags.Replace(decoded, string.Empty);
            var normalized = Regex.Replace(noTags, @"\s+", " ").Trim();

            return normalized;
        }
    }
}
