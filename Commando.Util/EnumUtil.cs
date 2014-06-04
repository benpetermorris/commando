using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace twomindseye.Commando.Util
{
    public static class EnumUtil
    {
        public static IEnumerable<string> GetEnumFlagStringValues<T>(T enumValue, bool emptyIfZero = true)
        {
            var intValue = (int) (object) enumValue;
            var type = typeof(T);
            var names = Enum.GetNames(type);
            var values = Enum.GetValues(type).Cast<int>();
            var zipped = names.Zip(values, (n, v) => new { n, v });

            if (intValue == 0)
            {
                return emptyIfZero ? Enumerable.Empty<string>() : zipped.Where(x => x.v == 0).Take(1).Select(x => x.n);
            }

            return from p in zipped where p.v != 0 && (intValue & p.v) == p.v select p.n;
        }
    }
}
