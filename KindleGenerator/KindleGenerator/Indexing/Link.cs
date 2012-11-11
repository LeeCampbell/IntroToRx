using System;
using System.Collections.Generic;
using System.Linq;

namespace KindleGenerator.Indexing
{
    public sealed class Link
    {
        public Link Parent { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string PageHref
        {
            get
            {
                var path = Href;
                var anchorIdx = path.IndexOf("#");
                if (anchorIdx > -1) path = path.Substring(0, anchorIdx);
                return path;
            }
        }
        private readonly List<Link> _subLinks = new List<Link>();
        public List<Link> SubLinks { get { return _subLinks; } }

        public bool Contains(string href)
        {
            return string.Equals(PageHref, href, StringComparison.InvariantCultureIgnoreCase) 
                || SubLinks.Any(subLink => subLink.Contains(href));
        }

        private static readonly LinkPageComparer linkPageComparer = new LinkPageComparer();
        public static IEqualityComparer<Link> PageComparer
        {
            get { return linkPageComparer; }
        }

        private sealed class LinkPageComparer :IEqualityComparer<Link>
        {
            public bool Equals(Link x, Link y)
            {
                return string.Equals(x.PageHref, y.PageHref, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(Link obj)
            {
                return obj.PageHref.GetHashCode();
            }
        }
    }
}