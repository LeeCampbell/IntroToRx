using System;
using System.Collections.Generic;
using System.Linq;

namespace KindleGenerator.Indexing
{
    public static class EnumerableEx
    {
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childSelector)
        {
            return from item in source
                   from descendant in new[] { item }.Concat(childSelector(item))
                   select descendant;
        }
    }
}