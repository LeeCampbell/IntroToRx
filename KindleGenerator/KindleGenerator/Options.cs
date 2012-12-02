using CommandLine;
using CommandLine.Text;

namespace KindleGenerator
{
    public class Options : CommandLineOptionsBase
    {
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        //OutputFormatFormat=MOBI|WebSite
        [Option("f", "outputformat", DefaultValue = OutputFormat.WebSite, HelpText = "The format the output should b generated as.")]
        public OutputFormat OutputFormat { get; set; }

        //-SourceDir=[Path to source files]
        [Option("s", "sourceDir", DefaultValue = "", HelpText = "The directory path to source files.")]
        public string SourceDirectory { get; set; }

        //-OutputDir=[Path to place output]
        [Option("o", "outputDir", DefaultValue = "", HelpText = "The directory path to place the output.")]
        public string OutputDirectory { get; set; }

        //-Author=[Lee Campbell]
        [Option("a", "author", DefaultValue = "", HelpText = "The Author of the content. Used in kindle format.")]
        public string Author { get; set; }

        //-BookName=[used as a file name for MOBI format e.g IntroToRx]
        [Option("n", "bookName", DefaultValue = "", HelpText = "The name of the book. Used as a file name for MOBI format e.g IntroToRx-->IntroToRx.mobi")]
        public string BookName { get; set; }

        ////-BookTitle=[e.g Introduction To Rx]
        [Option("t", "bookTitle", DefaultValue = "", HelpText = "The title of the book. Used in kindle format. e.g 'Introduction To Rx'")]
        public string BookTitle { get; set; }

        ////-BookSummary=[e.g. An introduction to the Microsoft's Reactive Extensions (Rx).]
        [Option("u", "bookSummary", DefaultValue = "", HelpText = "A summary of the the book. Used in kindle format. e.g 'An introduction to the Microsoft's Reactive Extensions (Rx).'")]
        public string BookSummary { get; set; }

        ////-CoverImage=[Path relative to SourceDir for MOBI Cover image]
        [Option("i", "coverImage", DefaultValue = "", HelpText = "Path relative to sourceDir for book cover image. Used in kindle format.")]
        public string CoverImage { get; set; }
        
        ////-Publisher=[e.g. Amazon.com]
       [Option("p", "publisher", DefaultValue = "Amazon.com", HelpText = "Publisher of the book. Used in kindle format.")]
        public string Publisher { get; set; }

    }

    public enum OutputFormat
    {
        MOBI,
        WebSite
        //PDF,
        //DOCX,
        //HTML
    }
}