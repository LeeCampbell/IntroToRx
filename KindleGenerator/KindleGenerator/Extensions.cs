using System;
using System.IO;

namespace KindleGenerator
{
    public static class Extensions
    {
        public static string[] SplitByLine(this string input)
        {
            return input.Replace("\r\n", "\n")
                .Split(new[] { "\n" }, StringSplitOptions.None);
        }

        public static bool IsNumeric(this string input)
        {
            int _;
            return Int32.TryParse(input, out _);
        }

        public static bool IsContent(this string filePath)
        {
            var file = new FileInfo(filePath);
            var prefix = file.Name.Substring(0, 2);
            return IsNumeric(prefix);
        }
    }
}