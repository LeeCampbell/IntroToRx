using KindleGenerator.CodeFormatting;
using NUnit.Framework;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class CSharpToXmlParserFixture
    {
        [Test]
        public void Should_Wrap_input_in_div_tag_with_csharpcode_class()
        {
            var input = "var i = 5;";
            var expected = "<div class=\"csharpcode\">\r\nvar i = 5;\r\n</div>";

            var actual = new CSharpToXmlParser().Parse(input);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_escape_content_to_xmlencoded()
        {
            var input = "var i = Observable.Return<string>(\"Some text\");";
            var expected = @"<div class=""csharpcode"">
var i = Observable.Return&lt;string&gt;(&quot;Some text&quot;);
</div>";

            var actual = new CSharpToXmlParser().Parse(input);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_trim_leading_whitespace_by_shortest_leading_whitespace()
        {
            var input = @"        var i = Observable.Return<string>(""Some text"");
        if(1==2)
        {
            b = false;
        }";
            var expected = @"<div class=""csharpcode"">
var i = Observable.Return&lt;string&gt;(&quot;Some text&quot;);
if(1==2)
{
    b = false;
}
</div>";
            var actual = new CSharpToXmlParser().Parse(input);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_trim_blank_lines_from_start_and_end()
        {
            var input = @"

var i = Observable.Return<string>(""Some text"");
if(1==2)
{
    b = false;
}
            ";
            var expected = @"<div class=""csharpcode"">
var i = Observable.Return&lt;string&gt;(&quot;Some text&quot;);
if(1==2)
{
    b = false;
}
</div>";
            var actual = new CSharpToXmlParser().Parse(input);
            Assert.AreEqual(expected, actual);
        }
    }
}