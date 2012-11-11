using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace KindleGenerator.CodeFormatting
{
    public abstract class WordWrapParserBase : IParser
    {
        public abstract string CssClass { get; }
        public abstract IEnumerable<string> Words { get; }
        protected virtual string Suffix { get { return string.Empty; } }

        public string RegexPattern
        {
            get { return string.Format(@"\b({0})\b", string.Join("|", Words)); }
        }

        public virtual bool IsReplacementCandidate(XElement xElement)
        {
            //Should not wrap anything if it is in a string literal or comment/remark
            return !xElement.Attributes().Any(att => att.Name == "class" && (att.Value == "str" || att.Value == "rem"));
        }

        public string Parse(string input)
        {
            var sb = new StringBuilder();
            var xElement = XElement.Parse(input, LoadOptions.PreserveWhitespace);

            AppendAsOpenTag(sb, xElement);
            ProcessNodes(xElement.Nodes(), sb, true);
            AppendAsCloseTag(sb, xElement);

            return sb.ToString();
        }

        private void ProcessNodes(IEnumerable<XNode> nodes, StringBuilder sb, bool performReplacements)
        {
            foreach (XNode node in nodes)
            {
                var xelement = node as XElement;
                if (xelement != null)
                {
                    ProcessXElement(xelement, sb, performReplacements);
                    continue;
                }
                var xtext = node as XText;
                if (xtext != null)
                {
                    ProcessXText(xtext, sb, performReplacements);
                    continue;
                }
                throw new InvalidOperationException("node type is not supported : " + node.GetType());
            }
        }

        private void ProcessXElement(XElement xElement, StringBuilder sb, bool performReplacements)
        {
            performReplacements = performReplacements && IsReplacementCandidate(xElement);

            AppendAsOpenTag(sb, xElement);
            ProcessNodes(xElement.Nodes(), sb, performReplacements);
            AppendAsCloseTag(sb, xElement);
        }

        private void ProcessXText(XText xtext, StringBuilder sb, bool performReplacements)
        {
            //.NET likes to just drop out some of the formatting, specially the &quot; which becomes normal old double quotes.
            var correctedText = XmlTextEncoder.Encode(xtext.Value);
            var newvalue = performReplacements ? Replace(correctedText) : correctedText;
            sb.Append(newvalue);
        }

        private string Replace(string value)
        {
            return Regex.Replace(value, RegexPattern, m => string.Format("<span class=\"{1}\">{0}</span>{2}", m.Value, CssClass, Suffix));
        }

        private static void AppendAsOpenTag(StringBuilder sb, XElement xElement)
        {
            sb.Append("<");
            sb.Append(xElement.Name);
            foreach (var xAttribute in xElement.Attributes())
            {
                sb.AppendFormat(" {0}=\"{1}\"", xAttribute.Name, xAttribute.Value);
            }
            sb.Append(">");
        }

        private static void AppendAsCloseTag(StringBuilder sb, XElement xElement)
        {
            sb.AppendFormat("</{0}>", xElement.Name);
        }
    }
}