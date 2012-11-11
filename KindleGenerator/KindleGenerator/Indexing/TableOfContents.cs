using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace KindleGenerator.Indexing
{
    //TODO: Take into account the class="SectionHeader" to break up the toc in to sections.
    //public class TableOfContents : HeadingIndexerBase
    //{
    //    public static void GenerateFile(string contentRoot)
    //    {
    //        var self = new TableOfContents();
    //        self.Generate(contentRoot);
    //    }

    //    protected override string TargetFile
    //    {
    //        get { return "toc.html"; }
    //    }

    //    protected override XName LevelTag
    //    {
    //        get { return XName.Get("ol"); }
    //    }

    //    protected override string OpenLevelTag(string fileName, string anchor, int index)
    //    {
    //        return "<ul>";
    //    }

    //    protected override string CloseLevelTag()
    //    {
    //        return "</ul>";
    //    }

    //    protected override string FileHeader()
    //    {
    //        var sb = new StringBuilder();
    //        sb.Append(@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd""");
    //        sb.Append(@">");
    //        sb.AppendLine(@"<html xmlns=""http://www.w3.org/1999/xhtml"">");
    //        sb.AppendLine(@"<head>");
    //        sb.AppendLine(@"<title>Table of Contents</title>");
    //        sb.AppendLine(@"<link rel=""stylesheet"" href=""Kindle.css"" type=""text/css"" />");
    //        sb.AppendLine(@"</head>");
    //        sb.AppendLine(@"<body>");
    //        sb.AppendLine(@"<div>");
    //        sb.AppendLine(@"<h1><b>TABLE OF CONTENTS</b></h1>");
    //        sb.AppendLine(@"<hr/>");
    //        sb.AppendLine(@"<ol class=""toc"">");
    //        return sb.ToString();
    //    }

    //    protected override string FileFooter()
    //    {
    //        var sb = new StringBuilder();
    //        sb.AppendLine(@"</ol>");
    //        sb.AppendLine(@"<hr />");
    //        sb.AppendLine(@"</div>");
    //        sb.AppendLine(@"</body>");
    //        sb.AppendLine(@"</html>");
    //        return sb.ToString();
    //    }

    //    protected override string ItemSelector(string fileName, string anchor, string heading, int nestingDepth)
    //    {
    //        var itemBuilder = new StringBuilder();
    //        itemBuilder.Append("<li>");
    //        itemBuilder.Append(@"<a href=""");
    //        itemBuilder.Append(fileName);
    //        itemBuilder.Append("#");
    //        itemBuilder.Append(anchor);
    //        itemBuilder.Append(@""">");
    //        itemBuilder.Append(heading);
    //        itemBuilder.Append(@"</a>");
    //        itemBuilder.Append("</li>");
    //        return itemBuilder.ToString();
    //    }
    //}

    public class TableOfContents
    {
        public static void GenerateFile(string contentRoot)
        {
            var content = new StringBuilder();
            content.Append(FileHeader());
            content.Append(FileContents(contentRoot));
            content.Append(FileFooter());

            var tocHtml = XDocument.Parse(content.ToString());

            //Console.WriteLine(content.ToString());

            var target = Path.Combine(contentRoot, TargetFile);
            tocHtml.WriteToFile(target);
        }

        private static string FileContents(string contentRoot)
        {
            var links = new NavigationTree(contentRoot).GetRootHeadings();

            var sb = new StringBuilder();

            foreach (var link in links)
            {
                sb.AppendLine("<div style=\"margin-top:1em;\">");
                sb.AppendFormat("<b><a href='{0}'>{1}</a></b>", link.Href, link.Name);
                sb.AppendLine();
                sb.Append(ProcessLevel1Link(link.SubLinks));
                sb.AppendLine("</div>");
            }
            return sb.ToString();
        }

        private static int level1Counter = 1;
        private static string ProcessLevel1Link(IList<Link> links)
        {
            if (links.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            
            foreach (var link in links)
            {
                sb.AppendFormat("<div>{2}. <a href='{0}'>{1}</a>", link.Href, link.Name, level1Counter++);
                sb.AppendLine();
                sb.Append(ProcessSubLinks(link.SubLinks));
                sb.AppendLine("</div>");
            }
            return sb.ToString();
        }

        private static string ProcessSubLinks(List<Link> subLinks)
        {
            if (subLinks.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("<ul>");
            foreach (var link in subLinks)
            {
                sb.AppendFormat("<li><a href='{0}'>{1}</a>", link.Href, link.Name);
                sb.AppendLine();
                sb.Append(ProcessSubLinks(link.SubLinks));
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul>");

            return sb.ToString();
        }

        private static string TargetFile
        {
            get { return "toc.html"; }
        }

        private static string FileHeader()
        {
            var sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd""");
            sb.Append(@">");
            sb.AppendLine(@"<html xmlns=""http://www.w3.org/1999/xhtml"">");
            sb.AppendLine(@"<head>");
            sb.AppendLine(@"<title>Table of Contents</title>");
            sb.AppendLine(@"<link rel=""stylesheet"" href=""Kindle.css"" type=""text/css"" />");
            sb.AppendLine(@"</head>");
            sb.AppendLine(@"<body>");
            sb.AppendLine(@"<div>");
            sb.AppendLine(@"<h1><b>TABLE OF CONTENTS</b></h1>");
            sb.AppendLine(@"<hr/>");
            //sb.AppendLine(@"<ol class=""toc"">");
            return sb.ToString();
        }

        private static string FileFooter()
        {
            var sb = new StringBuilder();
            //sb.AppendLine(@"</ol>");
            sb.AppendLine(@"<hr />");
            sb.AppendLine(@"</div>");
            sb.AppendLine(@"</body>");
            sb.AppendLine(@"</html>");
            return sb.ToString();
        }
    }
}
