using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace KindleGenerator
{
    public static class XLinqExtensions
    {
        public static bool IsHeading(this XElement element)
        {
            if (element.NodeType != XmlNodeType.Element) return false;
            var elementName = element.Name.ToString();
            if (!elementName.StartsWith("h")) return false;
            var headingClassAtt = element.Attribute("class");
            if (headingClassAtt != null)
            {
                if (headingClassAtt.Value.Split(' ').Any(v => v == "ignoreToc")) return false;
            }
            return elementName.Substring(1, 1).IsNumeric();
        }

        public static int HeadingLevel(this XElement element)
        {
            var lvlString = element.Name.ToString().Substring(1, 1);
            return Int32.Parse(lvlString);
        }

        public static void WriteToFile(this XDocument doc, string target)
        {
            using (var writer = XmlWriter.Create(target,
                new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = " ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    NewLineOnAttributes = false,
                    ConformanceLevel = ConformanceLevel.Document,

                    CloseOutput = true,
                    OmitXmlDeclaration = true
                }))
            {
                doc.WriteTo(writer);
                writer.Flush();
            }
        }

        //XDocument.Load(string) fails as it does not understand &nbsp; &zwnj; etc....
        public static XDocument LoadXml(this string file)
        {
            try
            {
                var content = File.ReadAllText(file);
                content = content.ReplaceNonSupportedEntities();
                return XDocument.Parse(content);
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException("Error processing file " + file, innerException);
            }
        }

        public static XElement ParseXml(this string content)
        {
            try
            {
                content = content.ReplaceNonSupportedEntities();
                return XElement.Parse(content, LoadOptions.PreserveWhitespace);
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException("Error processing xml snippet :" + content, innerException);
            }
        }

        public static XDocument ParseXmlDoc(this string content)
        {
            try
            {
                content = content.ReplaceNonSupportedEntities();
                return XDocument.Parse(content);
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException("Error processing xml snippet :" + content, innerException);
            }
        }

        public static string ReplaceNonSupportedEntities(this string xmlContent)
        {
            return xmlContent.Replace("&nbsp;", "&#160;")
                             .Replace("&zwnj;", "&#8204;")
                             .Replace("&eacute;", "&#233;");
        }

        public static string EncodeUnicodeCharacters(this string xmlContent)
        {
            var unicodeCharacters = new[]
                                        {
                                            8204, //&zwnj;  zero-width non joiner
                                            8217, //’       right single quotation mark
                                            8220, //“       left double quotation mark
                                            8221, //”       right double quotation mark
                                            233,  //é       Lowercase E-acute
                                            160,  //&nbsp;  non breaking space
                                        };

            return unicodeCharacters.Aggregate(
                xmlContent,
                (current, unicodeCharacter) =>
                    current.Replace(
                        Char.ConvertFromUtf32(unicodeCharacter),
                        string.Format("&#{0};", unicodeCharacter)));
        }

        public static string AsText(this XElement xElement)
        {
            return Regex.Replace(xElement.ToString(), "<.*?>", m => string.Empty);
        }
    }
}