using KindleGenerator.CodeFormatting;
using NUnit.Framework;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class ScopeAndLineParserFiture
    {
        [Test]
        public void Should_wrap_indented_lines_with_scope_div()
        {
            var input =
                @"<div class=""csharpcode"">
if(1<2)
{
    var i = Observable.Return&lt;string&gt;(&quot;Some text&quot;);
}
</div>";
            var expected =
                @"<div class=""csharpcode"">
<div class=""line"">if(1<2)</div>
<div class=""line"">{</div>
<div class=""scope"">
    <div class=""line"">var i = Observable.Return&lt;string&gt;(&quot;Some text&quot;);</div>
</div>
<div class=""line"">}</div>
</div>";

            var actual = new ScopeAndLineParser().Parse(input);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_wrap_indented_lines_with_scope_div2()
        {
            var input =
                @"<div class=""csharpcode"">
Observable
    .Return&lt;string&gt;(&quot;Some text&quot;);
</div>";
            var expected =
                @"<div class=""csharpcode"">
<div class=""line"">Observable</div>
<div class=""scope"">
    <div class=""line"">.Return&lt;string&gt;(&quot;Some text&quot;);</div>
</div>
</div>";

            var actual = new ScopeAndLineParser().Parse(input);
            Assert.AreEqual(expected, actual);
        }
    }
}