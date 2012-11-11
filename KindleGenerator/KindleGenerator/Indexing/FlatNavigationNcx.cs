using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace KindleGenerator.Indexing
{
    /// <summary>
    /// Constructs the flat ncx (only H1 &H2 titles) structure as supported by Calibre. We like Calibre because it creates better code samples for kindle
    /// </summary>
    public class FlatNavigationNcx : HeadingIndexerBase
    {
        private readonly string _title;
        private readonly string _author;
        private readonly string _targetFile;

        public FlatNavigationNcx(string title, string author, string targetFile)
        {
            _title = title;
            _author = author;
            _targetFile = targetFile;
        }

        public static void Generate(string contentRoot, string title, string author, string targetFile)
        {
            var self = new FlatNavigationNcx(title, author, targetFile);
            self.Generate(contentRoot);
        }

        protected override string TargetFile { get { return _targetFile; } }

        protected override XName LevelTag { get { return XName.Get("navPoint"); } }

        protected override string FileHeader()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE ncx PUBLIC ""-//NISO//DTD ncx 2005-1//EN"" ""http://www.daisy.org/z3986/2005/ncx-2005-1.dtd""[]>
<!--
	For a detailed description of NCX usage please refer to:
	http://www.idpf.org/2007/opf/OPF_2.0_final_spec.html#Section2.4.1
-->
<ncx xmlns=""http://www.daisy.org/z3986/2005/ncx/"" version=""2005-1"" xml:lang=""en-US"">
  <head>
    <meta name=""dtb:uid"" content=""BookId"" />
    <meta name=""dtb:depth"" content=""2"" />
    <meta name=""dtb:totalPageCount"" content=""0"" />
    <meta name=""dtb:maxPageNumber"" content=""0"" />
  </head>
  <docTitle>
    <text>" + _title + @"</text>
  </docTitle>
  <docAuthor>
    <text>" + _author + @"</text>
  </docAuthor>
  <navMap>
    <navPoint class=""toc"" id=""toc"" playOrder=""1"">
      <navLabel>
        <text>Table of Contents</text>
      </navLabel>
      <content src=""toc.html"" />
    </navPoint>";
        }

        protected override string FileFooter()
        {
            return @"  </navMap>
</ncx>";
        }

        protected override string ItemSelector(string fileName, string anchor, string heading, int nestingDepth)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<navLabel><text>{0}</text></navLabel>", heading);
            sb.AppendLine();
            sb.AppendFormat("<content src=\"{0}#{1}\" />", fileName, anchor);
            sb.AppendLine();
            return sb.ToString();
        }

        protected override string OpenLevelTag(string fileName, string anchor, int index)
        {
            var id = fileName.Substring(3);                     //remove the "nn_" prefix.
            id = id.Substring(0, fileName.LastIndexOf('.') - 3);    //Remove the extension.
            if (!string.IsNullOrEmpty(anchor))
            {
                id = id + "_" + anchor;
            }
            return string.Format(@"<navPoint class=""chapter"" id=""{0}"" playOrder=""{1}"">", id, index);
        }

        protected override IEnumerable<string> TableOfContents(IEnumerable<string> files)
        {
            var headings = from file in files
                           from tag in file.LoadXml().Descendants()
                           where tag.IsHeading()
                           select new { tag, file };

            var index = 0;

            return headings.Where(h => h.tag.HeadingLevel() < 3)
                .Select(h =>
                            {

                                string fileName = new FileInfo(h.file).Name;
                                var anchor = GetAnchor(h.tag);
                                var heading = h.tag.Value.Replace("<", "&lt;").Replace(">", "&gt;");

                                var itemValue = ItemSelector(fileName, anchor, heading, 0);
                                return new StringBuilder()
                                    .Append(OpenLevelTag(fileName, anchor, ++index))
                                    .Append(itemValue)
                                    .Append(CloseLevelTag())
                                    .ToString();
                            });

            var headingList = headings.ToList();
            return headingList
                .Zip(
                    headingList
                        .Skip(1)
                        .Concat(new[]{
                                         new { tag=new XElement("H1"), file=string.Empty}
                                     }),
                    (left, right) =>
                        {
                            index++;
                            var currentTag = left.tag;
                            var nextTag = right.tag;
                            var currentLevel = currentTag.HeadingLevel();
                            var nextLevel = nextTag.HeadingLevel();
                            var nestingDepthDelta = nextLevel - currentLevel;
                            var anchor = GetAnchor(currentTag);
                            var itemBuilder = new StringBuilder();
                            string fileName = new FileInfo(left.file).Name;
                            var heading = currentTag.Value.Replace("<", "&lt;").Replace(">", "&gt;");

                            //Always open a new level ie
                            /*
    <navPoint class="chapter" id="Forward_IntroductiontoRx" playOrder="2">
      <navLabel>
        <text>Introduction to Rx</text>
      </navLabel>
      <content src="00_Forward.html#IntroductiontoRx" />*/

                            //if next is same level then close
                            //if next is higher level then close again.


                            //Next tag is a sub heading
                            //for (int i = 0; i <= nestingDepthDelta; i++)  //Should only be 0 or 1.
                            //{
                            itemBuilder.Append(OpenLevelTag(fileName, anchor, index));
                            //}

                            var itemValue = ItemSelector(fileName, anchor, heading, currentLevel);
                            itemBuilder.Append(itemValue);

                            //This is the last of a sub heading
                            for (int i = nestingDepthDelta; i <= 0; i++)    //Should only be -1 or 0.
                            {
                                itemBuilder.Append(CloseLevelTag());
                            }

                            return itemBuilder.ToString();
                        });
        }
    }
}