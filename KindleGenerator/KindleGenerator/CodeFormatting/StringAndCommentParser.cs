using System;
using System.Text.RegularExpressions;

namespace KindleGenerator.CodeFormatting
{
    public class StringAndCommentParser : IParser
    {
        private readonly string _pattern;
        const string MultiLineBlockCommentPattern = @"/\*[\s\S]*?\*/";
        const string SingleLineBlockCommentPattern = @"/\*.*?\*/";
        const string LineCommentPattern = @"//.*?(?=(\r|$))";
        const string BlockStringPattern = @"@&quot;[\s\S]*?&quot;";
        const string LineStringPattern = @"&quot;.*?&quot;";


        public StringAndCommentParser()
        {

            _pattern = string.Format("(?<remark>({0}|{1}))|(?<string>({2}|{3}))|((?<leadingSpace>^\\s*)(?<leadingText>.*?)(?<blockComment>({4}))(?<trailingText>.*?)(?<trailingSpace>\\s*$))",
                                        SingleLineBlockCommentPattern, LineCommentPattern,
                                        BlockStringPattern, LineStringPattern,
                                        MultiLineBlockCommentPattern);
        }

        public string Parse(string input)
        {
            return Regex.Replace(input, _pattern, MatchEvaluator, RegexOptions.Multiline);
        }

        private static string MatchEvaluator(Match match)
        {
            string result;
            if (TryReplaceRemark(match, out result))
            {
                return result;
            }

            if (TryReplaceString(match, out result))
            {
                return result;
            }

            if (TryReplaceBlockComment(match, out result))
            {
                return result;
            }


            throw new InvalidOperationException("Unknown group has been matched.");
        }

        private static bool TryReplaceRemark(Match match, out string result)
        {
            var remarkMatch = match.Groups["remark"];
            if (remarkMatch.Success)
            {
                const string remPrefix = "<span class=\"rem\">";
                const string remSuffix = "</span>";
                result = string.Format("{0}{1}{2}", remPrefix, remarkMatch.Value, remSuffix);
                return true;
            }
            result = null;
            return false;
        }

        private static bool TryReplaceString(Match match, out string result)
        {
            var stringMatch = match.Groups["string"];
            if (stringMatch.Success)
            {
                const string strPrefix = "<span class=\"str\">";
                const string strSuffix = "</span>";
                result = string.Format("{0}{1}{2}", strPrefix, stringMatch.Value, strSuffix);
                return true;
            }
            result = null;
            return false;
        }

        private static bool TryReplaceBlockComment(Match match, out string result)
        {

            var leadingSpaceMatch = match.Groups["leadingSpace"];
            var leadingTextMatch = match.Groups["leadingText"];
            var blockCommentMatch = match.Groups["blockComment"];
            var trailingSpaceMatch = match.Groups["trailingSpace"];
            var trailingTextMatch = match.Groups["trailingText"];

            if (leadingSpaceMatch.Success && leadingTextMatch.Success && blockCommentMatch.Success)
            {
                var prefix = string.Empty;
                var suffix = string.Empty;
                var indent = leadingSpaceMatch.Length;
                var padding = new String(' ', indent);

                if (!string.IsNullOrEmpty(leadingTextMatch.Value)
                    && !string.IsNullOrEmpty(leadingSpaceMatch.Value))
                {
                    prefix = string.Format("{0}{1}\r\n", leadingSpaceMatch.Value, leadingTextMatch.Value);
                }

                //If there is trainling text, then move to next line.
                if (!string.IsNullOrEmpty(trailingTextMatch.Value))
                {
                    //This would be some shitty code if there was a line of code after the closing of a multiline block comment.
                    using (new ConsoleColor(System.ConsoleColor.Red))
                    {
                        Console.WriteLine("Hit this shitty line of code");
                    }
                    suffix = string.Format("\r\n{0}", trailingTextMatch.Value);
                }
                //If there was any trailing space then
                if (!string.IsNullOrEmpty(trailingSpaceMatch.Value))
                {
                    suffix = string.Format("{0}{1}", suffix, trailingSpaceMatch.Value);
                }

                string strPrefix = string.Format("{0}<div class=\"rem\">\r\n{0}", padding);
                string strSuffix = string.Format("\r\n{0}</div>{1}", padding, suffix);
                result = string.Format("{0}{1}{2}{3}", prefix, strPrefix, blockCommentMatch.Value, strSuffix);
                return true;
            }
            result = null;
            return false;
        }
    }
}