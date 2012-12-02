using System;
using System.IO;
using System.Linq;
using CommandLine;

namespace KindleGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var cmdLineOptions = new Options();
            if (CommandLineParser.Default.ParseArguments(args, cmdLineOptions))
            {
                // consume values here
                //Console.WriteLine("Author={0}", cmdLineOptions.Author);
                //Console.WriteLine("BookName={0}", cmdLineOptions.BookName);
                //Console.WriteLine("BookSummary={0}", cmdLineOptions.BookSummary);
                //Console.WriteLine("BookTitle={0}", cmdLineOptions.BookTitle);
                //Console.WriteLine("CoverImage={0}", cmdLineOptions.CoverImage);
                //Console.WriteLine("OutputDirectory={0}", cmdLineOptions.OutputDirectory);
                //Console.WriteLine("OutputFormat={0}", cmdLineOptions.OutputFormat);
                //Console.WriteLine("Publisher={0}", cmdLineOptions.Publisher);
                //Console.WriteLine("SourceDirectory={0}", cmdLineOptions.SourceDirectory);

                var isHelpRequired = false;
                try
                {
                    switch (cmdLineOptions.OutputFormat)
                    {
                        case OutputFormat.MOBI:
                            if (ValidateMobiArguments(cmdLineOptions))
                            {
                                Console.WriteLine("Kindle generation starting...");
                                GenerateKindleBook(cmdLineOptions.BookName, cmdLineOptions.BookTitle, cmdLineOptions.BookSummary,
                                                   cmdLineOptions.Author, cmdLineOptions.Publisher,
                                                   cmdLineOptions.SourceDirectory, cmdLineOptions.OutputDirectory);
                                Console.WriteLine("Kindle generation complete.");
                                
                            }
                            else
                            {
                                isHelpRequired = true;
                            }

                            break;
                        case OutputFormat.WebSite:
                            if (ValidateWebSiteArguments(cmdLineOptions))
                            {
                                Console.WriteLine("Web generation starting...");
                                GenerateWebContent(cmdLineOptions.SourceDirectory, cmdLineOptions.OutputDirectory);
                                Console.WriteLine("Web generation complete.");
                            }
                            else
                            {
                                isHelpRequired = true;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);

                    isHelpRequired = true;
                }
                if (isHelpRequired)
                {
                    Console.WriteLine();
                    Console.WriteLine(cmdLineOptions.GetUsage());

                    Environment.ExitCode = -1;
                }
            }
            else
            {
                Console.WriteLine("Incorrect usage. ");
                Console.WriteLine(cmdLineOptions.GetUsage());
                Environment.ExitCode = -1;
            }
        }

        private static bool ValidateMobiArguments(Options cmdLineOptions)
        {
            if (!ValidateCoreArguments(cmdLineOptions))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(cmdLineOptions.BookName))
            {
                Console.WriteLine("bookName is required for Mobi format");
                return false;
            }
            if (string.IsNullOrWhiteSpace(cmdLineOptions.BookTitle))
            {
                Console.WriteLine("bookTitle is required for Mobi format");
                return false;
            }
            if (string.IsNullOrWhiteSpace(cmdLineOptions.BookSummary))
            {
                Console.WriteLine("bookSummary is required for Mobi format");
                return false;
            }
            if (string.IsNullOrWhiteSpace(cmdLineOptions.Author))
            {
                Console.WriteLine("author is required for Mobi format");
                return false;
            }
            if (string.IsNullOrWhiteSpace(cmdLineOptions.Publisher))
            {
                Console.WriteLine("publisher is required for Mobi format");
                return false;
            }

            return true;
        }
        private static bool ValidateWebSiteArguments(Options cmdLineOptions)
        {
            return ValidateCoreArguments(cmdLineOptions);
        }
        private static bool ValidateCoreArguments(Options cmdLineOptions)
        {
            if (string.IsNullOrWhiteSpace(cmdLineOptions.SourceDirectory))
            {
                Console.WriteLine("sourceDir is required");
                return false;
            }
            if (string.IsNullOrWhiteSpace(cmdLineOptions.OutputDirectory))
            {
                Console.WriteLine("outputDir is required");
                return false;
            }
            return true;
        }

        private static void GenerateKindleBook(string bookName, string bookTitle, string bookSummary, string author, string publisher, string sourceDir, string targetDir)
        {
            //TODO: Validate content files    --Check that is starts with an H1, ends with an HR, and that Headings don't jump i.e. prevent H1 followed by H3.
            //Copy content files to the build dir for modification. i.e. We don't modify the source, we update then generate from that 
            CopyDirectory(sourceDir, targetDir);

            //TODO: Strip out non-kindle content i.e. .Where(x => !x.Attributes().Any(att => att.Name=="class" &&  att.Value.Split(' ').Contains("kindleOnly"))))

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
