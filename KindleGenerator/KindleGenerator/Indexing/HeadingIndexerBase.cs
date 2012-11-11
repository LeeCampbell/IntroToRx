using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace KindleGenerator.Indexing
{
    public abstract class HeadingIndexerBase
    {
        protected abstract string TargetFile { get; }

        protected abstract XName LevelTag { get; }

        protected abstract string FileHeader();

        protected abstract string FileFooter();

        protected abstract string ItemSelector(string fileName, string anchor, string heading, int nestingDepth);

        //TODO: Return as a TreeStructure. This should allows us to apply etter logc eleswhere.
        protected virtual IEnumerable<string> TableOfContents(IEnumerable<string> files)
        {
            var headings = from file in files
                           from tag in file.LoadXml().Descendants()
                           where tag.IsHeading()
                           select new { tag, file };

            var index = 0;
            var headingList = headings.ToList();
            return headingList
                .Zip(
                    headingList
                        .Skip(1)
                        .Concat(new[]{new { tag=new XElement("H1"), file=string.Empty}}),
                    (left, right) =>
                    {
                        index++;
                        var currentTag = left.tag;
                        var nextTag = right.tag;
                        var currentLevel = currentTag.HeadingLevel();
                        var nextLevel = nextTag.HeadingLevel();
                        var nestingDepthDelta = currentLevel - nextLevel;
                        var anchor = GetAnchor(currentTag);
                        var itemBuilder = new StringBuilder();
                        string fileName = new FileInfo(left.file).Name;
                        var heading = currentTag.Value.Replace("<", "&lt;").Replace(">", "&gt;");

                        if (anchor != null)
                        {
                            string itemValue = ItemSelector(fileName, anchor, heading, currentLevel);
                            itemBuilder.Append(itemValue);
                        }

                        //Next tag is a sub heading
                        for (int i = nestingDepthDelta; i < 0; i++)
                        {
                            itemBuilder.Append(OpenLevelTag(fileName, anchor, index));
                        }

                        //This is the last of a sub heading
                        for (int i = nestingDepthDelta; i > 0; i--)
                        {
                            itemBuilder.Append(CloseLevelTag());
                        }

                        return itemBuilder.ToString();
                    });
        }

        protected virtual string OpenLevelTag(string fileName, string anchor, int index)
        {
            return string.Format("<{0}>", LevelTag.LocalName);
        }

        protected virtual string CloseLevelTag()
        {
            return string.Format("</{0}>", LevelTag.LocalName);
        }

        public void Generate(string contentRoot)
        {
            var target = Path.Combine(contentRoot, TargetFile);
            var files = Directory.EnumerateFiles(contentRoot, "*.html")
                .Where(file => file.IsContent());

            IEnumerable<string> toc = TableOfContents(files);

            XDocument tocHtml = GenerateFile(toc);

            tocHtml.WriteToFile(target);
        }

        protected XDocument GenerateFile(IEnumerable<string> toc)
        {
            var tocList = toc.ToList();
            var sb = new StringBuilder();
            sb.AppendLine(FileHeader());
            tocList.ForEach(line => sb.AppendLine(line));
            sb.AppendLine(FileFooter());

            var tocHtml = XDocument.Parse(sb.ToString());

            //Remove empty tags
            var emptyElements = from element in tocHtml.Descendants()
                                where element.IsEmpty || string.IsNullOrWhiteSpace(element.Value)
                                where element.Name.LocalName.Equals(LevelTag.LocalName)
                                select element;

            while (emptyElements.Any())
                emptyElements.Remove();
            return tocHtml;
        }

        protected static string GetAnchor(XElement element)
        {
            var previous = element.PreviousNode as XElement; 
            
            XAttribute anchorNameAtt = null;
            if (previous != null && previous.Name.ToString().Equals("a", StringComparison.InvariantCultureIgnoreCase))
            {
                anchorNameAtt = previous.Attribute("name");
            }
            if (anchorNameAtt == null)
            {
                var headingClassAtt = element.Attribute("class");
                if (headingClassAtt != null)
                {
                    if (headingClassAtt.Value.Split(' ').Any(v => v == "ignoreToc"))
                    {
                        return null;
                    }
                }
                throw new InvalidOperationException("Anchor has no name on " + element);
            }
            return anchorNameAtt.Value;
        }
    }
}