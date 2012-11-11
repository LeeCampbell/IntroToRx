using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KindleGenerator.Indexing
{
    public class Manifest
    {
        private readonly string _bookName;
        private readonly string _title;
        private readonly string _description;
        private readonly string _creator;
        private readonly string _publisher;
        private readonly string _coverRelativePath;

        public static void Generate(string rootPath, string bookName, string title, string description, string creator, string publisher, string coverRelativePath)
        {
            var self = new Manifest(bookName, title, description, creator, publisher, coverRelativePath);
            self.Generate(rootPath);
        }

        private Manifest(string bookName, string title, string description, string creator, string publisher, string coverRelativePath)
        {
            _bookName = bookName;
            _title = title;
            _description = description;
            _creator = creator;
            _publisher = publisher;
            _coverRelativePath = coverRelativePath;
        }

        public void Generate(string rootPath)
        {
            var sb = new StringBuilder();
            sb.Append(Header());
            sb.AppendLine(Metadata());
            sb.Append(ManifestTable(rootPath));
            sb.AppendLine(Guide(rootPath));
            sb.Append(Footer());

            var manifestFile = Path.Combine(rootPath, _bookName + ".opf");
            File.WriteAllText(manifestFile, sb.ToString());
        }

        private static string Header()
        {
            return
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<!--
  The unique identifier in <package unique-identifier=”XYZ”> is a reference to
  the identifier specified in <metadata> as <dc:Identifier id=”XYZ”>.
-->
<package xmlns=""http://www.idpf.org/2007/opf"" version=""2.0"" unique-identifier=""BookId"">";
        }

        private string Metadata()
        {
            const string format = @"
    <!--
	Metadata:
	The required metadata element is used to provide information about the publication
	as a whole.
	
	For detailed info visit: http://www.idpf.org/2007/opf/OPF_2.0_final_spec.html#Section2.2
    -->
	
    <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
  
        <!-- Title [mandatory]: The title of the publication. This is the title that will appear on the ""Home"" screen. -->
  
	    <dc:title>{0}</dc:title>
  
        <!-- Language [mandatory]: the language of the publication. The language codes used are the same as in XML
        and HTML. The full list can be found here: http://www.w3.org/International/articles/language-tags/
        Some common language strings are:
        ""en""    English
        ""en-us"" English - USA
        ""en-gb"" English - United Kingdom
        ""fr""    French
        ""fr-ca"" French - Canada
        ""de""    German
        ""es""    Spanish
        -->
	    <dc:language>en-us</dc:language>

        <!-- Cover [mandatory]. The cover image must be specified in <manifest> and referenced from
        this <meta> element with a name=""cover"" attribute.
        -->
        <meta name=""cover"" content=""My_Cover"" />
  
        <!-- The ISBN of your book goes here -->
        <!--dc:identifier id=""BookId"" opf:scheme=""ISBN"">9781375890815</dc:identifier-->
  
        <!-- The author of the book. For multiple authors, use multiple <dc:Creator> tags.
            Additional contributors whose contributions are secondary to those listed in
            creator  elements should be named in contributor elements.
        -->
        <dc:creator>{1}</dc:creator>
  
        <!-- Publisher: An entity responsible for making the resource available -->
        <dc:publisher>{2}</dc:publisher>
  
        <!-- Subject: A topic of the content of the resource. Typically, Subject will be
		        expressed as keywords, key phrases or classification codes that describe a topic
		        of the resource. The BASICCode attribute should contain the subject code
            according to the BISG specification:
            http://www.bisg.org/what-we-do-20-73-bisac-subject-headings-2008-edition.php
        -->
        <dc:subject>Reference</dc:subject>
  
        <!-- Date: Date of publication in YYYY-MM-DD format. (Days and month can be omitted).
            Standard to follow: http://www.w3.org/TR/NOTE-datetime
        -->
        <dc:date>{3:yyyy-MM-dd}</dc:date>

        <!-- Description: A short description of the publication's content. -->
        <dc:description>{4}</dc:description>
    </metadata>";
            return string.Format(format, _title, _creator, _publisher, DateTime.Today, _description);
        }

        private string ManifestTable(string contentPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<manifest>");
            AppendCss(sb, contentPath);
            int contentFileCount = AppendHtmlContent(sb, contentPath);

            sb.AppendLine(@"  <!-- table of contents [mandatory] -->");
            sb.AppendFormat(@"  <item id=""My_Table_of_Contents"" media-type=""application/x-dtbncx+xml"" href=""{0}.ncx""/>", _bookName);
            sb.AppendLine();
            sb.AppendLine(@"  <!-- cover image [mandatory] -->");
            sb.AppendFormat(@"  <item id=""My_Cover"" media-type=""image/jpeg"" href=""{0}""/>", _coverRelativePath);
            sb.AppendLine();
            sb.AppendLine(@"</manifest>");
            sb.AppendLine("");
            sb.AppendLine(@"<!--");
            sb.AppendLine(@"	Spine:");
            sb.AppendLine(@"	Following manifest, there must be one and only one spine element, which contains one");
            sb.AppendLine(@"	or more itemref elements. Each itemref references an document designated");
            sb.AppendLine(@"	in the manifest. The order of the itemref elements organizes the associated content");
            sb.AppendLine(@"	files into the linear reading order of the publication.");
            sb.AppendLine(@"	");
            sb.AppendLine(@"	The toc attribute refers to the id ref of the NCX file specified in the manifest.");
            sb.AppendLine(@"	");
            sb.AppendLine(@"	For detailed info visit: ");
            sb.AppendLine(@"		http://www.idpf.org/2007/opf/OPF_2.0_final_spec.html#Section2.4");
            sb.AppendLine(@"		http://www.niso.org/workrooms/daisy/Z39-86-2005.html#NCX");
            sb.AppendLine(@"-->");
            sb.AppendLine(@"	");
            sb.AppendLine(@"<spine toc=""My_Table_of_Contents"">");
            sb.AppendLine(@"  <!-- the spine defines the linear reading order of the book -->");
            for (int i = 0; i < contentFileCount; i++)
            {
                sb.AppendFormat(@"	<itemref idref=""item{0}""/>", i);
                sb.AppendLine();
            }
            sb.AppendLine(@"</spine>");
            return sb.ToString();
        }

        private int AppendHtmlContent(StringBuilder sb, string contentPath)
        {
            int i = 0;
            sb.AppendLine("  <!-- HTML content files [mandatory] -->");
            var contentFiles = Directory.EnumerateFiles(contentPath, "*.html").Where(IsContentFile);
            
            sb.AppendFormat(@"	<item id=""item{0}"" media-type=""application/xhtml+xml"" href=""toc.html""></item>", i++);
            sb.AppendLine();

            foreach (var contentFile in contentFiles)
            {
                var fileName = new FileInfo(contentFile).Name;
                sb.AppendFormat(@"	<item id=""item{0}"" media-type=""application/xhtml+xml"" href=""{1}""></item>", i++, fileName);
                sb.AppendLine();
            }
            return i;
        }

        private void AppendCss(StringBuilder sb, string contentPath)
        {
            int x = 0;
            
            sb.AppendLine("  <!-- HTML css files -->");
            var cssFiles = Directory.EnumerateFiles(contentPath, "*.css");

            foreach (var contentFile in cssFiles)
            {
                var fileName = new FileInfo(contentFile).Name;
                sb.AppendFormat(@"	<item id=""css{0}"" media-type=""text/css"" href=""{1}""></item>", x++, fileName);
                sb.AppendLine();
            }
        }

        private static string Guide(string contentPath)
        {
            var firstFile = Directory.EnumerateFiles(contentPath, "*.html")
                                     .Where(IsContentFile)
                                     .First();
            var firstFileName = new FileInfo(firstFile).Name;
            var format = @"<!--
	Guide:
	Within the package there may be one guide element, containing one or more reference elements.
	The guide element identifies fundamental structural components of the publication, to enable
	Reading Systems to provide convenient access to them.
  
  For detailed info visit: http://www.idpf.org/2007/opf/OPF_2.0_final_spec.html#Section2.6
  
  The Kindle reading system support two special guide items which are both mandatory.
  type=""toc""  [mandatory]: a link to the HTML table of contents
  type=""text"" [mandatory]: a link to where the content of the book starts (typically after the front matter)
	
  Kindle reading platforms need both these guide items to provide a consistent user experience to the user.
  
  It is good practice to include both a logical table of contents (NCX) and an HTML table of contents
  (made of hyperlinks). The NCX enables various advanced navigation features but the HTML table of
  contents can easily be discovered by the user by paging through the book. Both are useful.
	
-->
	
<guide>
	<reference type=""toc"" title=""Table of Contents"" href=""toc.html""></reference>
	<reference type=""text"" title=""Forward"" href=""{0}""></reference>
</guide>";
            return string.Format(format, firstFileName);
        }

        private static string Footer()
        {
            return @"</package>";
        }

        private static bool IsContentFile(string path)
        {
            var fileName = new FileInfo(path).Name;
            return Regex.IsMatch(fileName, @"\d\d_.*\.html");
        }
    }
}
