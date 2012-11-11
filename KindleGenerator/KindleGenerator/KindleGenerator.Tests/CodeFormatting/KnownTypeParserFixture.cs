using KindleGenerator.CodeFormatting;
using NUnit.Framework;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class KnownTypeParserFixture
    {
        private static readonly string[] _knownTypes = new[] { "IDisposable", "Observable", "Console" };
        private KnownTypeParser _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new KnownTypeParser(_knownTypes);
        }

        [Test]
        public void Should_wrap_Knowntypes()
        {
            var input = "<div class=\"csharpcode\">\nIDisposable i = Subscribe();\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"type\">IDisposable</span> i = Subscribe();\n</div>";
            var actual = _sut.Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_not_wrap_keywords_in_comments()
        {
            var input = "<div class=\"csharpcode\">\r\nIDisposable i = Subscribe();<span class=\"rem\">// Disposable should not be wrapped as it is in a comment</span>\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"type\">IDisposable</span> i = Subscribe();<span class=\"rem\">// Disposable should not be wrapped as it is in a comment</span>\n</div>";
            var actual = _sut.Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_not_wrap_keywords_in_stringLiterals()
        {
            var input = "<div class=\"csharpcode\">\r\nIDisposable i = Subscribe(<span class=\"str\">&quot;this Disposable should not be wrapped&quot;</span>);\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"type\">IDisposable</span> i = Subscribe(<span class=\"str\">&quot;this Disposable should not be wrapped&quot;</span>);\n</div>";
            var actual = _sut.Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_maintain_quote_gt_and_lt_markup()
        {
            var input = "<div class=\"csharpcode\">\r\nIDisposable i = Subscribe(<span class=\"str\">&quot;this Disposable should not be wrapped. 1 &lt; 2&quot;</span>);\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"type\">IDisposable</span> i = Subscribe(<span class=\"str\">&quot;this Disposable should not be wrapped. 1 &lt; 2&quot;</span>);\n</div>";
            var actual = _sut.Parse(input);

            Assert.AreEqual(expected, actual);
        }
    }
}