using System;
using System.IO;
using System.Linq;

namespace KindleGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO: get the console arg parsers from codeproject/codeplex.

            var rootPath = args[0];
            var bookName = args[1];

            //TODO: ValidatePath(rootPath);
            //Change all the <a name><h1../></a> to be <a name/><h1></h1>. Fail.

            try
            {
                Console.WriteLine("Kindle generation starting...");
                GenerateKindleBook(rootPath, bookName);
                Console.WriteLine("Kindle generation complete.");
                Console.WriteLine("Web generation starting...");
                GenerateWebContent(rootPath);
                Console.WriteLine("Web generation complete.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void GenerateKindleBook(string rootPath, string bookName)
        {
            //Validate content files    --Check that is starts with an H1, ends with an HR, and that Headings don't jump i.e. prevent H1 followed by H3.

            //Copy content files to the build dir for modification. i.e. We don't modify the source, we update then generate from that 
            var contentPath = Path.Combine(rootPath, "content");
            var outputPath = Path.Combine(rootPath, "bin");
            var targetPath = Path.Combine(outputPath, "content");
            CopyDirectory(contentPath, targetPath);

            CodeFormatting.CodeFormatter.FormatKindleContentFiles(targetPath);
            Indexing.TableOfContents.GenerateFile(targetPath);
            //Indexing.NestedNavigationNcx.Generate(targetPath, "Introduction to Rx", "Lee Campbell", bookName + ".ncx");
            Indexing.FlatNavigationNcx.Generate(targetPath, "Introduction to Rx", "Lee Campbell", bookName + ".ncx");
            Indexing.Manifest.Generate(targetPath, bookName, "Introduction to Rx", "An introduction to the Microsoft's Reactive Exentions (Rx).", "Lee Campbell", "Amazon.com", "GraphicsIntro\\Cover.jpg");
        }
        private static void GenerateWebContent(string rootPath)
        {
            var contentPath = Path.Combine(rootPath, "content");
            var targetPath = Path.Combine(rootPath, @"WebSite\IntroToRx\content\v1.0.10621.0");
            CopyDirectory(contentPath, targetPath);

            Indexing.WebFormatter.FormatContentFiles(targetPath);
            CodeFormatting.CodeFormatter.FormatWebContentFiles(targetPath);
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
