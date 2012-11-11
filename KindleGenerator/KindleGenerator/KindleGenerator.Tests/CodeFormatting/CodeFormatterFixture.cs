using System.Text.RegularExpressions;
using KindleGenerator.CodeFormatting;
using NUnit.Framework;
using System.IO;
using System;

namespace KindleGenerator.Tests.CodeFormatting
{
    [TestFixture]
    public class CodeFormatterFixture
    {
        [Test]
        public void Should_pass_input_to_xmlWrapper()
        {
            var input =
@"if(1<2)
{
    var i = Observable.Return<string>(""Some text"");
}";

            var expected =
                @"<div class=""csharpcode"">
<div class=""line""><span class=""kwrd"">if</span>(&zwnj;1&lt;2)</div>
<div class=""line"">{</div>
<div class=""scope"">
    <div class=""line""><span class=""kwrd"">var</span> i = <span class=""type"">Observable</span>&zwnj;.Return&lt;<span class=""kwrd"">string</span>&gt;(&zwnj;<span class=""str"">&quot;Some text&quot;</span>);</div>
</div>
<div class=""line"">}</div>
</div>";

            var actual = CodeFormatter.Format(input, CodeFormatter.KindleParsers);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_()
        {
            var input = "<div class=\"csharpcode\">\r\n  <div class=\"line\" style=\"text-indent:-10pt\">\r\n    <span class=\"kwrd\">static</span>\r\n    <span class=\"kwrd\">void</span> Main(<span class=\"kwrd\">string</span>[] args)</div>\r\n  <div class=\"line\">{</div>\r\n  <div class=\"scope\">\r\n    <div class=\"line\">var subject = <span class=\"kwrd\">new</span> <span class=\"type\">Subject</span>&lt;<span class=\"kwrd\">string</span>&gt;();</div>\r\n    <div class=\"line\">WriteStreamToConsole(subject);</div>\r\n    <div class=\"line\">subject.OnNext(<span class=\"str\">\"a\"</span>);</div>\r\n    <div class=\"line\">subject.OnNext(<span class=\"str\">\"b\"</span>);</div>\r\n    <div class=\"line\">subject.OnNext(<span class=\"str\">\"c\"</span>);</div>\r\n    <div class=\"line\">Console.ReadKey();</div>\r\n  </div>\r\n  <div class=\"line\">}</div>\r\n  <div class=\"line\">\r\n    <span class=\"kwrd\">static</span>\r\n    <span class=\"kwrd\">void</span> WriteStreamToConsole(<span class=\"type\">IObservable</span>&lt;<span class=\"kwrd\">string</span>&gt; stream)</div>\r\n  <div class=\"line\">{</div>\r\n  <div class=\"scope\">\r\n    <div class=\"line\">stream.Subscribe(Console.WriteLine);</div>\r\n  </div>\r\n  <div class=\"line\">}</div>\r\n</div>";
            var expected = "\r\n  \r\n    static\r\n    void Main(string[] args)\r\n  {\r\n  \r\n    var subject = new Subject&lt;string&gt;();\r\n    WriteStreamToConsole(subject);\r\n    subject.OnNext(\"a\");\r\n    subject.OnNext(\"b\");\r\n    subject.OnNext(\"c\");\r\n    Console.ReadKey();\r\n  \r\n  }\r\n  \r\n    static\r\n    void WriteStreamToConsole(IObservable&lt;string&gt; stream)\r\n  {\r\n  \r\n    stream.Subscribe(Console.WriteLine);\r\n  \r\n  }\r\n";

            var actual = Regex.Replace(input, "<.*?>", m => string.Empty);
	
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_handle_block_comments_correctly()
        {
            var input = @"public static void RetrySample&lt;T&gt;(IObservable&lt;T&gt; stream)
                        {
                            stream.Retry().Subscribe(t=&gt;Console.WriteLine(t)); //Will always retry the stream
                            Console.ReadKey();
                        }
                        /*Given stream that will produce 0,1,2 then error; the output would be
                        0
                        1
                        2
                        0
                        1
                        2
                        0
                        1
                        2.....*/";
            var expected =
@"<div class=""csharpcode"">
<div class=""line""><span class=""kwrd"">public</span> <span class=""kwrd"">static</span> <span class=""kwrd"">void</span> RetrySample&amp;lt;T&amp;gt;(&zwnj;<span class=""type"">IObservable</span>&amp;lt;T&amp;gt; stream)</div>
<div class=""scope"">
                        <div class=""line"">{</div>
                        <div class=""scope"">
                            <div class=""line"">stream&zwnj;.Retry(&zwnj;)&zwnj;.Subscribe(&zwnj;t=&amp;gt;<span class=""type"">Console</span>&zwnj;.WriteLine(&zwnj;t)); <span class=""rem"">//Will always retry the stream</span></div>
                            <div class=""line""><span class=""type"">Console</span>&zwnj;.ReadKey(&zwnj;);</div>
                        </div>
                        <div class=""line"">}</div>
                        <div class=""rem"">
                        <div class=""line"">/*Given stream that will produce 0,1,2 then error; the output would be</div>
                        <div class=""line"">0</div>
                        <div class=""line"">1</div>
                        <div class=""line"">2</div>
                        <div class=""line"">0</div>
                        <div class=""line"">1</div>
                        <div class=""line"">2</div>
                        <div class=""line"">0</div>
                        <div class=""line"">1</div>
                        <div class=""line"">2&zwnj;.&zwnj;.&zwnj;.&zwnj;.&zwnj;.*/</div>
                        </div>
</div>
</div>";

            var actual = CodeFormatter.Format(input, CodeFormatter.KindleParsers);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestsyPoo()
        {
            //copy dummy file to temp location
            //format it 
            //debug why the spans are being joined.
            //.\Content\*.html
            //.\kindlegen\KindleGen\Bin\Debug
            //..\..\..\..\content\*.html

            var d = Environment.CurrentDirectory;
            File.Delete("01_KeyTypes.html");
            File.Copy(@"..\..\..\..\..\content\01_KeyTypes.html", "01_KeyTypes.html");
            CodeFormatter.FormatKindleContentFiles(@".\");
        }
    }
}
