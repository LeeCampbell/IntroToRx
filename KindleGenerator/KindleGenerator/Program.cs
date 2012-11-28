using System;
using System.IO;
using System.Linq;

namespace KindleGenerator
{
    public enum OutputTarget
    {
        MOBI,
        WebSite
        //PDF,
        //DOCX,
        //HTML
    }

    class Program
    {
        //TODO: get the console arg parsers from codeproject/codeplex.
        //-OutputTargetFormat=MOBI|WebSite
        //-SourceDir=[Path to source files]
        //-OutputDir=[Path to place output]
        //-Author=[Lee Campbell]
        //-BookName=[used as a file name for MOBI format e.g IntroToRx]
        //-BookTitle=[e.g Introduction To Rx]
        //-BookSummary=[e.g. An introduction to the Microsoft's Reactive Extensions (Rx).]
        //-CoverImage=[Path relative to SourceDir for MOBI Cover image]
        //-Publisher=[e.g. Amazon.com]

        static void Main(string[] args)
        {
            var rootPath = args[0];
            var bookName = args[1];


            var bookTitle = "Introduction to Rx";
            var bookSummary = "An introduction to the Microsoft's Reactive Extensions (Rx).";
            var author = "Lee Campbell";
            var publisher = "Amazon.com";
            var sourceDir = Path.Combine(rootPath, "content");

            //TODO: ValidatePath(rootPath);
            //Change all the <a name><h1../></a> to be <a name/><h1></h1>. Fail.

            try
            {
                Console.WriteLine("Kindle generation starting...");
                GenerateKindleBook(bookName, bookTitle, bookSummary, author, publisher, sourceDir, Path.Combine(Path.Combine(rootPath, "bin"), "content"));
                Console.WriteLine("Kindle generation complete.");
                Console.WriteLine("Web generation starting...");
                GenerateWebContent(sourceDir, Path.Combine(rootPath, @"WebSite\content\v1.0.10621.0"));
                Console.WriteLine("Web generation complete.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void GenerateKindleBook(string bookName, string bookTitle, string bookSummary, string author, string publisher, string sourceDir, string targetDir)
        {
            //TODO: Validate content files    --Check that is starts with an H1, ends with an HR, and that Headings don't jump i.e. prevent H1 followed by H3.
            //Copy content files to the build dir for modification. i.e. We don't modify the source, we update then generate from that 
            CopyDirectory(sourceDir, targetDir);
            CodeFormatting.CodeFormatter.FormatKindleContentFiles(targetDir);
            Indexing.TableOfContents.GenerateFile(targetDir);
            //Indexing.NestedNavigationNcx.Generate(targetPath, "Introduction to Rx", "Lee Campbell", bookName + ".ncx");
            Indexing.FlatNavigationNcx.Generate(targetDir, bookTitle, author, bookName + ".ncx");
            Indexing.Manifest.Generate(targetDir, bookName, bookTitle, bookSummary, author, publisher, "GraphicsIntro\\Cover.jpg");
        }
        private static void GenerateWebContent(string sourceDir, string targetDir)
        {
            CopyDirectory(sourceDir, targetDir);
            Indexing.WebFormatter.FormatContentFiles(targetDir);
            CodeFormatting.CodeFormatter.FormatWebContentFiles(targetDir);
        }
        

        private static void CopyDirectory(string sourcePath, string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            try
            {
                Directory.CreateDirectory(targetPath);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to create dir : {0}", targetPath);
                throw;
            }
            
            
            var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories).ToList();
            var directories = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories).ToList();

            //Now Create all of the directories
            foreach (string dirPath in directories)
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

            //Copy all the files
            foreach (string newPath in files)
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath));
        }
    }
}
