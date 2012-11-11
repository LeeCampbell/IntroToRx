using KindleGenerator.CodeFormatting;
using NUnit.Framework;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class KeywordParserFixture
    {
        [Test]
        public void Should_wrap_keywords()
        {
            var input = "<div class=\"csharpcode\">\r\nvar i = 5;\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"kwrd\">var</span> i = 5;\n</div>";
            var actual = new KeywordParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_not_wrap_keywords_in_comments()
        {
            var input = "<div class=\"csharpcode\">\r\nvar i = 5;<span class=\"rem\">//this var should not be wrapped</span>\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"kwrd\">var</span> i = 5;<span class=\"rem\">//this var should not be wrapped</span>\n</div>";
            var actual = new KeywordParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_not_wrap_keywords_in_stringLiterals()
        {
            var input = "<div class=\"csharpcode\">\r\nvar i = <span class=\"str\">&quot;this var should not be wrapped&quot;</span>;\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"kwrd\">var</span> i = <span class=\"str\">&quot;this var should not be wrapped&quot;</span>;\n</div>";
            var actual = new KeywordParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_maintain_quote_gt_and_lt_markup()
        {
            var input = "<div class=\"csharpcode\">\r\nvar i = <span class=\"str\">&quot;1 &gt; 0!&quot;</span>;\r\n</div>";
            var expected = "<div class=\"csharpcode\">\n<span class=\"kwrd\">var</span> i = <span class=\"str\">&quot;1 &gt; 0!&quot;</span>;\n</div>";
            var actual = new KeywordParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }
    }
}