using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace twomindseye.Commando.Util
{
    public static class StringUtil
    {
        /// <summary>
        /// Compute the Levenshtein distance between two strings.
        /// </summary>
        public static int LevenshteinDistance(string a, string b)
        {
            // http://webreflection.blogspot.com/2009/02/levenshtein-algorithm-revisited-25.html
            if (a == b)
            {
                return 0;
            }
            
            if (a.Length == 0 || b.Length == 0)
            {
                return a.Length == 0 ? b.Length : a.Length;
            }

            int len1 = a.Length + 1;
            int len2 = b.Length + 1;
            int I = 0;
            int i = 0;
            int c;
            int j;
            int J;
            
            var d = new int[len1,len2];

            while (i < len2)
            {
                d[0, i] = i++;
            }
            
            i = 0;
            
            while (++i < len1)
            {
                J = j = 0;
                c = a[I];
                d[i, 0] = i;

                while (++j < len2)
                {
                    d[i, j] = Math.Min(Math.Min(d[I, j] + 1, d[i, J] + 1), d[I, J] + (c == b[J] ? 0 : 1));
                    ++J;
                }

                ++I;
            }

            return d[len1 - 1, len2 - 1];
        }

        public static byte[] HexToBytes(string s)
        {
            const string hexChars = "0123456789abcdef";

            if (s.Length % 2 == 1)
            {
                throw new ArgumentException();
            }

            var rvl = new byte[s.Length / 2];

            for (var i = 0; i < s.Length; i += 2)
            {
                var hi = hexChars.IndexOf(Char.ToLower(s[i]));
                var lo = hexChars.IndexOf(Char.ToLower(s[i + 1]));

                if (hi == -1 || lo == -1)
                {
                    throw new ArgumentException();
                }

                rvl[i/2] = (byte) (hi << 4 | lo);
            }

            return rvl;
        }
    }
}
