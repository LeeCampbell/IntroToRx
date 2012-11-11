using KindleGenerator.CodeFormatting;
using NUnit.Framework;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class ZeroWidthNonJoinerParserFixture
    {
        [Test]
        public void Should_Add_zerowidthNonJoiner_infront_of_dots_and_after_openbracket()
        {
            var input =
                @"<div class=""csharpcode"">
<div class=""line"">if(1<2)</div>
<div class=""line"">{</div>
<div class=""scope"">
    <div class=""line"">var i = Observable.Return&lt;string&gt;(&quot;Some text&quot;);</div>
</div>
<div class=""line"">}</div>
</div>";
            var expected =
                @"<div class=""csharpcode"">
<div class=""line"">if(&zwnj;1<2)</div>
<div class=""line"">{</div>
<div class=""scope"">
    <div class=""line"">var i = Observable&zwnj;.Return&lt;string&gt;(&zwnj;&quot;Some text&quot;);</div>
</div>
<div class=""line"">}</div>
</div>";
            var actual = new ZeroWidthNonJoinerParser().Parse(input);

            Assert.AreEqual(expected, actual);

        }
    }
}