using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KindleGenerator.CodeFormatting
{
    public class ScopeAndLineParser : IParser
    {
        public string Parse(string input)
        {
            //loop through each line
            //  compare indent level
            //  open or close scope as nessecary
            //  wrap line in line div

            var sb = new StringBuilder();
            var lines = input.SplitByLine();
            var indent = 0;
            var currentIndent = indent;
            var indents = new Stack<int>();
            indents.Push(indent);

            //TODO: Ignore the <div class="str">
            //                  <div class="rem">
            //                  </div>
            //      ie any line that is just a open/close tag. This would then do the <div class="csharpcode"> too.

            ////Assume the 1st line is <div class="csharpcode">
            //sb.AppendLine(lines.First());

            


            //foreach (var line in lines.Skip(1).Take(lines.Length - 2))
            foreach (var line in lines.Take(lines.Length - 1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (IsOnlyXmlTag(line))
                {
                    sb.AppendLine(line);
                    continue;
                }


                //Handle change in scope
                currentIndent = GetIndent(line);
                if (currentIndent > indent)
                {
                    indents.Push(currentIndent);
                    sb.Append(new string(' ', indent));
                    sb.AppendLine("<div class=\"scope\">");
                }
                while (currentIndent < indents.Peek())
                {
                    indents.Pop();
                    var thisIndent = indents.Peek();
                    sb.Append(new string(' ', thisIndent));
                    sb.AppendLine("</div>");
                }

                //Wrap line in a line div
                sb.Append(new string(' ', currentIndent));
                sb.Append("<div class=\"line\">");
                sb.Append(line.TrimStart());
                sb.AppendLine("</div>");

                indent = currentIndent;
            }

            indents.Pop();
            foreach (var i in indents)
            {
                sb.Append(new string(' ', i));
                sb.AppendLine("</div>");
            }

            sb.Append(lines.Last());

            return sb.ToString();
        }

        private bool IsOnlyXmlTag(string line)
        {
            return Regex.IsMatch(line, @"^\s*\<[A-Za-z0-9_ \=""\/]*\>\s*$");
        }

        private static int GetIndent(string line)
        {
            var match = Regex.Match(line, @"\S");
            return match.Index;
        }
    }
}