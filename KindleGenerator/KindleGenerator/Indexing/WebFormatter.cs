using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KindleGenerator.Indexing
{
    public static class WebFormatter
    {
        public static void FormatContentFiles(string targetPath)
        {
            //* Construct the Nav Tree
            var navigation = new NavigationTree(targetPath);
            var rootHeadings = navigation.GetRootHeadings().ToList();
            var contentFilePaths = Directory.GetFiles(targetPath, "*.html");

            foreach (var currentFilePath in contentFilePaths)
            {
                var newContents = new StringBuilder();

                newContents.Append(GenerateHeader(currentFilePath));
                newContents.Append(GenerateHeaderNavigation(currentFilePath, rootHeadings));
                newContents.Append(GenerateNavigation(currentFilePath, rootHeadings));
                newContents.Append(@"<div class=""main"">
                                        <div style=""float: right; margin: 10px 0px 10px 10px; width: 140px; font-size: 11px;text-align: center"">
                                            <img src=""../../Styles/IntroToRx_Cover.png"" style=""margin: 10px"" />
                                            Lee Campbell<br/>
                                            <a href=""IntroToRx.mobi"" title=""Introduction to Rx as .mobi file (for Kindle)"">Introduction to Rx<br/>Kindle edition</a> (2012)
                                        </div>");

                newContents.Append(GenerateBody(currentFilePath));
                newContents.Append(GenerateFooterNavigation(currentFilePath, rootHeadings));
                newContents.Append("</div>");
                newContents.Append(GenerateFooter());

                newContents.ToString()
                    .ParseXmlDoc()
                    .WriteToFile(currentFilePath);
            }
        }

        private static string GenerateHeader(string currentFilePath)
        {
            var content = File.ReadAllText(currentFilePath);
            var title = Regex.Match(content, @"<title>.*</title>");
            var sb = new StringBuilder();
            
//            var header =  @"
//<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
//<html xmlns=""http://www.w3.org/1999/xhtml"">
//<head>
//    {0}
//    <link href=""../../Styles/Site.css"" rel=""stylesheet"" type=""text/css"" />
//</head>
//<body>
//    <div class=""page"">
//        <div class=""header"">
//            <img id=""Img1"" src=""../../Styles/titleText.png"" style=""height: 80px; margin: 20px; float: left"" />
//            <span style=""float: right; margin: 80px 10px 5px 5px; "">
//                <img src=""../../Styles/Social.png"" />
//            </span>
//            
//            <div>
//
//<!--form method=""get"" action=""http://www.google.com/search"">
//    <div style=""border:1px solid black;padding:4px;width:20em;"">
//        <table border=""0"" cellpadding=""0"">
//            <tr>
//                <td>
//                    <input type=""text"" name=""q"" size=""25""  maxlength=""255"" value="""" />
//                    <input type=""submit"" value=""Google Search"" />
//                </td>
//            </tr>
//        </table>
//    </div>
//</form-->
//                <input type=""text"" style=""margin-top: 25px; width: 250px; font-style: italic; color: #707070""
//                    value=""Search..."" />
//                <br />
//                <span style=""color: #707070; font-weight: 700; font-size: 11px"">Version v1.0.1062.0</span>
//                <a style=""font-size: 11px"" href=""#"">Other versions<img src=""../../Styles/DropDown.png"" style=""margin-left:2px; margin-bottom: 2px"" /></a> 
//            </div>";
            
            var header = @"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    {0}
    <link href=""../../Styles/Site.css"" rel=""stylesheet"" type=""text/css"" />
    {1}
</head>
<body>
    <div class=""page"">
        <div class=""header"">
            <div style=""margin: 70px 20px 5px; float: right;text-align: right"">
                <a href=""http://twitter.com/#!/search/%23RxNET"">#RxNET</a><br/>
                <span style=""color: #707070; font-weight: 700; font-size: 11px;"">Version v1.0.1062.0</span> 
            </div>

            <img id=""Logo"" src=""../../Styles/titleText.png"" style=""height: 80px; margin: 20px; float: left"" />
            ";
            sb.AppendFormat(header, title, GoogleAnalyticsScript);
            return sb.ToString();
        }

        private static string GoogleAnalyticsScript
        {
            get
            {
                return @"
    <!--Google analytics-->
    <script type=""text/javascript"">

      var _gaq = _gaq || [];
      _gaq.push(['_setAccount', 'UA-32704657-1']);
      _gaq.push(['_trackPageview']);

      (function() {
        var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
        ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
        var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
      })();

    </script>";
            }
        }

        private static string GenerateHeaderNavigation(string contentFilePath, IList<Link> rootHeadings)
        {
            var fileName = contentFilePath.Substring(contentFilePath.LastIndexOf('\\') + 1);
            var sb = new StringBuilder();
            sb.AppendLine(@"<table class='toc' border='0' cellspacing='0' cellpadding='0'>");
            sb.AppendLine("<tr>");

            foreach (var root in rootHeadings)
            {
                var text = root.Name;
                var seperatorIdx = text.IndexOf(" - ");

                if (seperatorIdx > 0)
                {
                    text = text.Substring(0, seperatorIdx);
                }
                if (root.Contains(fileName))
                {
                    sb.AppendFormat("<th class='tocactive'>{0}</th>", text);
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendFormat("<th class='toc'><a class='toc' href='{1}'>{0}</a></th>", text, root.PageHref);
                    sb.AppendLine();
                }
            }
            sb.AppendLine("</tr>");
            sb.AppendLine("<tr>");
            foreach (var root in rootHeadings)
            {
                var text = string.Empty;// root.Name;
                var seperatorIdx = root.Name.IndexOf(" - ");

                if (seperatorIdx > 0)
                {
                    text = root.Name.Substring(seperatorIdx + 3);


                    if (root.Contains(fileName))
                    {

                        sb.AppendFormat("<td class='tocactive'>{0}</td>", text);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendFormat("<td class='toc'><a class='toc' href='{1}'>{0}</a></td>", text, root.PageHref);
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendFormat("<td></td>");
                    sb.AppendLine();
                }
            }
            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        private static string GenerateNavigation(string currentPath, IList<Link> rootHeadings)
        {
            var sb = new StringBuilder();
            var fileName = currentPath.Substring(currentPath.LastIndexOf('\\') + 1);
            sb.AppendLine(@"<div class=""leftCol"">");
            foreach (var root in rootHeadings)
            {
                sb.AppendLine(@"<ul class=""partMenu"">");
                sb.AppendLine(@"<li>");
                sb.AppendFormat(@"<div class=""partMenuHeader""><a href=""{1}"">{0}</a></div>", root.Name, root.PageHref);
                sb.AppendLine();
                if (root.Contains(fileName))
                {
                    sb.Append(GenerateCurrentNavigation(root, int.MaxValue));
                }
                else
                {
                    sb.Append(GenerateCurrentNavigation(root, 1));
                }
                sb.AppendLine("</li>");
                sb.AppendLine("</ul>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string GenerateCurrentNavigation(Link root, int maxDepth)
        {
            if (root.SubLinks.Count == 0 || maxDepth < 1) return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine("<ul>");
            foreach (var subLink in root.SubLinks)
            {
                sb.AppendFormat(@"<li><a href=""{1}"">{0}</a>", subLink.Name, subLink.Href);
                sb.AppendLine();
                sb.Append(GenerateCurrentNavigation(subLink, maxDepth - 1));
                sb.AppendLine("</li>");
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        private static string GenerateBody(string contentFilePath)
        {
            var sb = new StringBuilder();
            
            var html = contentFilePath.LoadXml();
            // Extract body
            var body = html.Element("html")
                           .Element("body");

            //Remove any kindle content
            foreach (var xElement in body.Elements()
                .Where(x => !x.Attributes().Any(att => att.Name=="class" &&  att.Value.Split(' ').Contains("kindleOnly"))))
            {
                var text = xElement.ToString();
                sb.Append(text.Replace("&zwnj;", string.Empty));
            }
            return sb.ToString();
        }

        private static string GenerateFooterNavigation(string currentFilePath, IList<Link> rootHeadings)
        {
            var fileName = currentFilePath.Substring(currentFilePath.LastIndexOf('\\') + 1);
            //flatten all the links, distinct, get the values either side.
            var allPageLinks = rootHeadings.Flatten(link => link.SubLinks)
                .Distinct(Link.PageComparer)
                .ToList();

            var idx = allPageLinks.FindIndex(
                    link => string.Equals(link.PageHref, fileName, StringComparison.InvariantCultureIgnoreCase));

            var sb = new StringBuilder();
            sb.Append("<table width='100%'><tr><td>");
            if (idx > 0)
            {
                sb.Append("&lt;&lt; Back to : ");
                sb.AppendFormat(@"<a href=""{1}"">{0}</a>", allPageLinks[idx - 1].Name, allPageLinks[idx - 1].PageHref);
            }
            sb.Append("</td><td></td><td align='right'>");
            if (idx + 1 < allPageLinks.Count)
            {
                sb.Append("Moving on to : ");
                sb.AppendFormat(@"<a href=""{1}"">{0}</a>", allPageLinks[idx + 1].Name, allPageLinks[idx + 1].PageHref);
                sb.Append("&gt;&gt;");
            }
            sb.Append("</td></tr></table>");
            return sb.ToString();
        }

        private static string GenerateFooter()
        {
            return @"
        <div class=""footer"">
            Original series @ <a href=""http://leecampbell.blogspot.com/2010/08/reactive-extensions-for-net.html"">Lee Campbell</a>
        </div>
    </div>
</body>
</html>";
        }
    }
}
