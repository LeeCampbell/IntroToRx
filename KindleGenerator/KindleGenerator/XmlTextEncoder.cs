using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KindleGenerator
{
    //http://stackoverflow.com/questions/157646/best-way-to-encode-text-data-for-xml
    /// <summary>
    /// Encodes data so that it can be safely embedded as text in XML documents.
    /// </summary>
    internal class XmlTextEncoder : TextReader
    {
        public static string Encode(string s)
        {
            using (var stream = new StringReader(s))
            using (var encoder = new XmlTextEncoder(stream))
            {
                return encoder.ReadToEnd();
            }
        }

        /// <param name="source">The data to be encoded in UTF-16 format.</param>
        /// <param name="filterIllegalChars">It is illegal to encode certain
        /// characters in XML. If true, silently omit these characters from the
        /// output; if false, throw an error when encountered.</param>
        public XmlTextEncoder(TextReader source, bool filterIllegalChars = true)
        {
            _source = source;
            _filterIllegalChars = filterIllegalChars;
        }

        readonly Queue<char> _buf = new Queue<char>();
        readonly bool _filterIllegalChars;
        readonly TextReader _source;

        public override int Peek()
        {
            PopulateBuffer();
            if (_buf.Count == 0) return -1;
            return _buf.Peek();
        }

        public override int Read()
        {
            PopulateBuffer();
            if (_buf.Count == 0) return -1;
            return _buf.Dequeue();
        }

        void PopulateBuffer()
        {
            const int endSentinel = -1;
            while (_buf.Count == 0 && _source.Peek() != endSentinel)
            {
                // Strings in .NET are assumed to be UTF-16 encoded [1].
                var c = (char)_source.Read();
                if (_entities.ContainsKey(c))
                {
                    // Encode all entities defined in the XML spec [2].
                    foreach (var i in _entities[c]) _buf.Enqueue(i);
                }
                else if (!(0x0 <= c && c <= 0x8) &&
                         !new[] { 0xB, 0xC }.Contains(c) &&
                         !(0xE <= c && c <= 0x1F) &&
                         !(0x7F <= c && c <= 0x84) &&
                         !(0x86 <= c && c <= 0x9F) &&
                         !(0xD800 <= c && c <= 0xDFFF) &&
                         !new[] { 0xFFFE, 0xFFFF }.Contains(c))
                {
                    // Allow if the Unicode codepoint is legal in XML [3].
                    _buf.Enqueue(c);
                }
                else if (char.IsHighSurrogate(c) &&
                         _source.Peek() != endSentinel &&
                         char.IsLowSurrogate((char)_source.Peek()))
                {
                    // Allow well-formed surrogate pairs [1].
                    _buf.Enqueue(c);
                    _buf.Enqueue((char)_source.Read());
                }
                else if (!_filterIllegalChars)
                {
                    // Note that we cannot encode illegal characters as entity
                    // references due to the "Legal Character" constraint of
                    // XML [4]. Nor are they allowed in CDATA sections [5].
                    throw new ArgumentException(
                        String.Format("Illegal character: '{0:X}'", (int)c));
                }
            }
        }

        static readonly Dictionary<char, string> _entities =
            new Dictionary<char, string> {
                                             { '"', "&quot;" }, 
                                             { '&', "&amp;"}, 
                                             { '\'', "&apos;" },
                                             { '<', "&lt;" }, 
                                             { '>', "&gt;" },
                                         };

        // References:
        // [1] http://en.wikipedia.org/wiki/UTF-16/UCS-2
        // [2] http://www.w3.org/TR/xml11/#sec-predefined-ent
        // [3] http://www.w3.org/TR/xml11/#charsets
        // [4] http://www.w3.org/TR/xml11/#sec-references
        // [5] http://www.w3.org/TR/xml11/#sec-cdata-sect
    }
}