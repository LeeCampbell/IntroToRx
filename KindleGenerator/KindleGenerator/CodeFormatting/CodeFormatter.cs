using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace KindleGenerator.CodeFormatting
{
    public class CodeFormatter
    {
        public static readonly IEnumerable<IParser> KindleParsers = new IParser[]
                                                             {
                                                                //wrap in <div class="csharpcode"/>
                                                                new CSharpToXmlParser(),
                                                                //find and wrap strings or comments (with equal precedence)
                                                                new StringAndCommentParser(),
                                                                //process keywords (not in comments or strings)
                                                                new KeywordParser(),
                                                                //process types (not in comments or strings)
                                                                new KnownTypeParser(),
                                                                //process scope and lines
                                                                new ScopeAndLineParser(),
                                                                //finally process zero-width-non-joiner across everything (even comments and strings)
                                                                new ZeroWidthNonJoinerParser()
                                                             };
        private static readonly IEnumerable<IParser> WebParsers = new IParser[]
                                                             {
                                                                //wrap in <div class="csharpcode"/>
                                                                new CSharpToXmlParser(),
                                                                //find and wrap strings or comments (with equal precedence)
                                                                new StringAndCommentParser(),
                                                                //process keywords (not in comments or strings)
                                                                new KeywordParser(),
                                                                //process types (not in comments or strings)
                                                                new KnownTypeParser(),
                                                                //process scope and lines
                                                                new ScopeAndLineParser(),
                                                             };


        public static void FormatKindleContentFiles(string contentRoot)
        {
            var contentFiles = GetContentFilePaths(contentRoot);
            FormatContent(contentFiles, KindleParsers);
        }
        public static void FormatWebContentFiles(string contentRoot)
        {
            var contentFiles = GetContentFilePaths(contentRoot);
            FormatContent(contentFiles, WebParsers);
        }

        private static string[] GetContentFilePaths(string rootPath)
        {
            return Directory.GetFiles(rootPath, "*.html", SearchOption.TopDirectoryOnly);
        }

        private static void FormatContent(IEnumerable<string> contentFiles, IEnumerable<IParser> parsers)
        {
            foreach (var file in contentFiles)
            {
                var doc = file.LoadXml();
                doc = FormatCodeSamples(doc, parsers);
                var newContent = doc.ToString().EncodeUnicodeCharacters();
                File.WriteAllText(file, newContent);
            }
        }

        private static XDocument FormatCodeSamples(XDocument doc, IEnumerable<IParser> parsers)
        {
            var xElements = doc.DescendantNodes().OfType<XElement>().ToList();
            var codeSamples = xElements.Where(
                node =>
                    node!=null 
                    && node.Attributes().Any(a => a.Name == "class" && a.Value == "csharpcode"));

            foreach (var codeSample in codeSamples)
            {
                var encodedCSharp = codeSample.AsText();
                var code = System.Net.WebUtility.HtmlDecode(encodedCSharp);
                var formattedCode = Format(code, parsers);
                var newContent = formattedCode.ParseXml();
                
                codeSample.ReplaceWith(newContent);
            }
            return doc;
        }

        public static string Format(string input, IEnumerable<IParser> parsers)
        {
            return parsers.Aggregate(input, (current, parser) => parser.Parse(current));
        }
    }
}