using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace twomindseye.Commando.Util
{
    public static class LinqExtensions
    {
        public static string CharsToString(this IEnumerable<char> chars)
        {
            return new string(chars.ToArray());
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> sequence, T item)
        {
            foreach (var i in sequence)
            {
                yield return i;
            }

            yield return item;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> sequence, T item)
        {
            yield return item;

            foreach (var i in sequence)
            {
                yield return i;
            }
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> sequence, IEnumerable<T> sequence2)
        {
            foreach (var i in sequence2)
            {
                yield return i;
            }

            foreach (var i in sequence)
            {
                yield return i;
            }
        }

        public static int FirstIndexWhere<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            var i = 0;

            foreach (var item in sequence)
            {
                if (predicate(item))
                {
                    return i;
                }

                ++i;
            }

            return -1;
        }

        public static string JoinStrings(this IEnumerable<string> sequence, string separator)
        {
            return string.Join(separator, sequence);
        }
    }
}
