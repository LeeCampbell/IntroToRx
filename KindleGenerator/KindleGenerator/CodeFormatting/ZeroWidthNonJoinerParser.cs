using System.Text.RegularExpressions;

namespace KindleGenerator.CodeFormatting
{
    /// <summary>
    /// Places the zero-width nonjoiner escape before dots (for fluent interfaces) and after open parentheses.
    /// This should allow code to autoformat in a natural way.
    /// </summary>
    /// <remarks>
    /// the intention is that a line of code that needs to be wrapped will favor wrapping on appropriate characters 
    /// eg: a snippet like 
    /// var xs = Observable.Interval(TimeSpan.FromSeconds(1).Select(i=>i.ToString());
    /// may need to wrap. Without the zero-width nonjoiner the code may wrap like this:
    /// var xs = Observable.Interval(TimeSpan.FromSecon
    ///     ds(1).Select(i=>i.ToString());
    /// 
    /// with the zero-width nonjoiners, it can display in each of the following formats depending on how much width is available
    /// 
    /// var xs = Observable.Interval(TimeSpan.FromSeconds(1).Select(i=>i
    ///     .ToString());
    /// 
    /// var xs = Observable.Interval(TimeSpan.FromSeconds(1).Select(
    ///     i=>i.ToString());
    /// 
    /// var xs = Observable.Interval(TimeSpan.FromSeconds(1)
    ///     .Select(i=>i.ToString());
    /// 
    /// var xs = Observable.Interval(TimeSpan
    ///     .FromSeconds(1).Select(i=>i.ToString());
    /// 
    /// var xs = Observable
    ///     .Interval(
    ///     TimeSpan
    ///     .FromSeconds(1)
    ///     .Select(
    ///     i=>i.ToString());
    /// 
    /// </remarks>
    public class ZeroWidthNonJoinerParser : IParser
    {
        //TODO: Swap &nbsp;--> &#160; and &zwnj; -->&#8204; as XLinq does not support these entities. ie use the Unicode substitutions instead.
        public string Parse(string input)
        {
            var replaced = Regex.Replace(input, @"\.", match=>"&zwnj;.");
            return Regex.Replace(replaced, @"\(", match => "(&zwnj;");
        }
    }
}