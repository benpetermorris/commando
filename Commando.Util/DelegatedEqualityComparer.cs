using System;
using System.Collections.Generic;

namespace twomindseye.Commando.Util
{
    public sealed class DelegatedEqualityComparer
    {
        public static IEqualityComparer<T> Create<T>(Func<T, T, bool> comparer)
        {
            return new Comparer<T>(comparer);
        }

        class Comparer<T> : IEqualityComparer<T>
        {
            readonly Func<T, T, bool> _comparer;

            public Comparer(Func<T, T, bool> comparer)
            {
                _comparer = comparer;
            }

            public bool Equals(T x, T y)
            {
                return _comparer(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
