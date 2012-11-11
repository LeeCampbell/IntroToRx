using System.Collections.Generic;

namespace KindleGenerator.CodeFormatting
{
    public class KeywordParser : WordWrapParserBase
    {
       private static readonly string[] _keywords = new[]
                                                            {
                                                                //Adding in var and the preprocessor directives
                                                                "var", //"#if", "#endif", "#region", "#endregion",
                                                                //Keywords from http://msdn.microsoft.com/en-us/library/x53a06bb(v=vs.71).aspx
                                                                "abstract", "event", "new", "struct",
                                                                "as", "explicit", "null", "switch",
                                                                "base", "extern", "object", "this",
                                                                "bool", "false", "operator", "throw",
                                                                "break", "finally", "out", "true",
                                                                "byte", "fixed", "override", "try",
                                                                "case", "float", "params", "typeof",
                                                                "catch", "for", "private", "uint",
                                                                "char", "foreach", "protected", "ulong",
                                                                "checked", "goto", "public", "unchecked",
                                                                "class", "if", "readonly", "unsafe",
                                                                "const", "implicit", "ref", "ushort",
                                                                "continue", "in", "return", "using",
                                                                "decimal", "int", "sbyte", "virtual",
                                                                "default", "interface", "sealed", "volatile",
                                                                "delegate", "internal", "short", "void",
                                                                "do", "is", "sizeof", "while",
                                                                "double", "lock", "stackalloc", 
                                                                "else", "long", "static", 
                                                                "enum", "namespace", "string",
                                                                
                                                                "from", "in", "select"//Query comprehension syntax highlighting
                                                            };

        public override string CssClass
        {
            get { return "kwrd"; }
        }

        public override IEnumerable<string> Words
        {
            get { return _keywords; }
        }
    }
}