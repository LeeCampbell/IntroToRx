using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace KindleGenerator.Indexing
{
    public class NavigationTree
    {
        private readonly string _contentRoot;

        public NavigationTree(string contentRoot)
        {
            _contentRoot = contentRoot;
        }

        public IEnumerable<Link> GetRootHeadings()
        {
            var files = Directory.EnumerateFiles(_contentRoot, "*.html")
                .Where(file => Extensions.IsContent(file));

            var headings = Headings(files)
                .Select(x =>
                            {
                                var level = GetLevel(x.Item1);
                                var path = x.Item2;
                                var name = x.Item1.Value.Replace("<", "&lt;").Replace(">", "&gt;");
                                return new { Level = level, Name = name, FilePath = path };
                            });


            var root = new Link { Level = -1 };
            var current = root;
            foreach (var heading in headings)
            {
                var newItem = new Link
                                  {
                                      Level = heading.Level,
                                      Name = heading.Name,
                                      Href = heading.FilePath,
                                  };
                Link parent = null;
                if (newItem.Level == current.Level)
                {
                    parent = current.Parent;
                }
                else if (newItem.Level == current.Level + 1)
                {
                    parent = current;
                }
                else if (newItem.Level < current.Level)
                {
                    parent = current.Parent;
                    while (newItem.Level <= parent.Level)
                    {
                        parent = parent.Parent;
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
                parent.SubLinks.Add(newItem);
                newItem.Parent = parent;
                current = newItem;
            }
            return root.SubLinks;
        }

        private static IEnumerable<Tuple<XElement, string>> Headings(IEnumerable<string> files)
        {
            return from file in files
                   from tag in file.LoadXml().Descendants()
                   where tag.IsHeading()
                   select Tuple.Create(tag, GetAnchor(tag, file));
        }

        protected static string GetAnchor(XElement element, string file)
        {
            var path = file.Substring(file.LastIndexOf('\\') + 1);

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
                        return path;
                    }
                }
                throw new InvalidOperationException("Anchor has no name on " + element);
            }
            return path + "#" + anchorNameAtt.Value;
        }

        public static int GetLevel(XElement element)
        {
            int level = Int32.Parse(element.Name.ToString().Substring(1, 1));

            if (level == 1)
            {
                var headingClassAtt = element.Attribute("class");
                if (headingClassAtt != null && headingClassAtt.Value.Split(' ').Any(v => v == "SectionHeader"))
                {
                    level--;
                }
            }
            return level;
        }
    }
}