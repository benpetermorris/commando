using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;

namespace Jint
{
    public class CachedTypeResolver : ITypeResolver
    {
        readonly ExecutionVisitor _visitor;
        static Dictionary<string, Type> _Cache = new Dictionary<string, Type>();
        static ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();

        public CachedTypeResolver(ExecutionVisitor visitor)
        {
            _visitor = visitor;
        }

        public Type ResolveType(string fullname)
        {
            Type type;

            rwl.EnterReadLock();

            try
            {
                type = _visitor.Usings.TryResolveType(fullname,
                                      n =>
                                      {
                                          Type rvl;
                                          return _Cache.TryGetValue(n, out rvl) ? rvl : null;
                                      });

                if (type != null)
                {
                    return type;
                }
            }
            finally
            {
                rwl.ExitReadLock();
            }

            type = _visitor.Usings.TryResolveType(fullname, Usings.TestAppDomainAssemblies);

            rwl.EnterWriteLock();

            try
            {
                _Cache.Add(fullname, type);
                return type;
            }
            finally
            {
                rwl.ExitWriteLock();
            }
        }
    }
}
