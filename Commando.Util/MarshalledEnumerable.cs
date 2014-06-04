using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace twomindseye.Commando.Util
{
    /// <summary>
    /// A marshallable implementation of IEnumerable&lt;T&lt; for use across appdomains.
    /// </summary>
    public sealed class MarshalledEnumerable<T> : MarshalByRefObject, IEnumerable<T>
    {
        readonly IEnumerable<T> _enumerable; 

        public MarshalledEnumerable(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable ?? Enumerable.Empty<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        sealed class Enumerator : MarshalByRefObject, IEnumerator<T>
        {
            readonly IEnumerator<T> _enumerator;

            public Enumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public T Current
            {
                get
                {
                    return _enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }
    }
}
