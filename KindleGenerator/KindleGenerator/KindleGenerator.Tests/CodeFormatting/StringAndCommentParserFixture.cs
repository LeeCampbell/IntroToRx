using KindleGenerator.CodeFormatting;
using NUnit.Framework;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class StringAndCommentParserFixture
    {
        [TestCase("i = 1; //Comment", "i = 1; <span class=\"rem\">//Comment</span>")]   //Last/single line of code
        [TestCase("i = 2; //Comment\r\ni = 2", "i = 2; <span class=\"rem\">//Comment</span>\r\ni = 2")] //Using esacpe chars for return-newLine.
        [TestCase(@"i = 3; //Comment
i = 3",
            @"i = 3; <span class=""rem"">//Comment</span>
i = 3")]//Using literals for return-newLine
        public void Should_wrap_line_comments(string input, string expected)
        {
            var actual = new StringAndCommentParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [TestCase("/* some comment */", "<span class=\"rem\">/* some comment */</span>")]
        [TestCase(@"i = 4;
/* some comment
    and another comment
*/
i = 4", @"i = 4;
<div class=""rem"">
/* some comment
    and another comment
*/
</div>
i = 4")]
        [TestCase(@"i = 5;
/* some comment
    and another comment
*/
i = 5
/* and another comment*/", @"i = 5;
<div class=""rem"">
/* some comment
    and another comment
*/
</div>
i = 5
<span class=""rem"">/* and another comment*/</span>")]
        public void Should_wrap_block_comments(string input, string expected)
        {
            var actual = new StringAndCommentParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [TestCase("mystring = &quot;This should be wrapped&quot;;", "mystring = <span class=\"str\">&quot;This should be wrapped&quot;</span>;")]
        public void Should_wrap_string_literals(string input, string expected)
        {
            var actual = new StringAndCommentParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [TestCase(@"mystring = @&quot;This should be wrapped
new line should be wrapped too.
&quot;;
i = 4",
@"mystring = <span class=""str"">@&quot;This should be wrapped
new line should be wrapped too.
&quot;</span>;
i = 4")]
        [TestCase(@"mystring = @&quot;This should be wrapped
new line should be wrapped too.
&quot;;
i = 5;
mystring = @&quot;This should be wrapped&quot;",
@"mystring = <span class=""str"">@&quot;This should be wrapped
new line should be wrapped too.
&quot;</span>;
i = 5;
mystring = <span class=""str"">@&quot;This should be wrapped&quot;</span>")]
        [TestCase(@"MethodCall(&quot;This should be wrapped&quot;, &quot;This should also be wrapped&quot;);",
            @"MethodCall(<span class=""str"">&quot;This should be wrapped&quot;</span>, <span class=""str"">&quot;This should also be wrapped&quot;</span>);")]
        public void Should_wrap_block_string_literals(string input, string expected)
        {
            var actual = new StringAndCommentParser().Parse(input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_push_multiline_block_comments_on_to_their_own_line()
        {
            //This is specifically to preserve valid xml. It also is a bit of a shitty style to start a block comment on a line of code and carry it on the next line.

            var input = @"    var i = 6; /* Start of comment on line 1
    * and then run on to the next line*/";

            var expected = @"    var i = 6; 
    <div class=""rem"">
    /* Start of comment on line 1
    * and then run on to the next line*/
    </div>";

            var actual = new StringAndCommentParser().Parse(input);

            Assert.AreEqual(expected, actual);

        }

    }
}