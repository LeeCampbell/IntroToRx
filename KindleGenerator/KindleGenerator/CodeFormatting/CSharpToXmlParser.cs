using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KindleGenerator.CodeFormatting
{
    public class CSharpToXmlParser : IParser
    {
        public string Parse(string input)
        {
            string trimmedInput = TrimBlankLines(input);
            trimmedInput = TrimUnessecaryIndents(trimmedInput);

            //TODO: remove all the leading whitespace.

            var encoded = XmlTextEncoder.Encode(trimmedInput);
            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"csharpcode\">");
            sb.AppendLine(encoded);
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string TrimUnessecaryIndents(string input)
        {
            var lines = input.SplitByLine();
            var minLeadingIndent = lines.Where(line=>!string.IsNullOrWhiteSpace(line))
                                        .Min(line => GetIndent(line));
            var sb = new StringBuilder();
            foreach (var line in lines.Take(lines.Length-1))
            {
                sb.AppendLine(TrimLine(line, minLeadingIndent));
            }
            sb.Append(TrimLine(lines.Last(), minLeadingIndent));

            return sb.ToString();
        }

        private static string TrimLine(string line, int indent)
        {
            if(string.IsNullOrWhiteSpace(line)) return string.Empty;
            return line.Substring(indent);
        }

        private static int GetIndent(string line)
        {
            var match = Regex.Match(line, @"\S");
            return match.Index;
        }

        private static string TrimBlankLines(string input)
        {
            var lines = input.SplitByLine();
            int leadingBlankLines = LeadingBlankLines(lines);
            int trailingBlankLines = TrailingBlankLines(lines);

            return string.Join("\r\n", lines.Skip(leadingBlankLines)
                                           .Take(lines.Length - leadingBlankLines - trailingBlankLines));
        }

        private static int LeadingBlankLines(string[] lines)
        {
            int i;
            for (i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    return i;
                }
            }
            return i;
        }

        private static int TrailingBlankLines(string[] lines)
        {
            int count = 0;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    return count;
                }
                count++;
            }
            return count;
        }
    }
}