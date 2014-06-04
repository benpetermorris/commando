using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Jint
{
    public sealed class Using
    {
        public string Namespace { get; private set; }
        public string Alias { get; private set; }

        public Using(string ns)
        {
            Namespace = ns;
        }

        public Using(string ns, string alias)
        {
            Namespace = ns;
            Alias = alias;
        }
    }

    public class Usings : IEnumerable<Using>
    {
        readonly List<Using> _usings = new List<Using>();

        public void Add(Using @using)
        {
            _usings.Add(@using);
        }

        public void CopyFrom(IEnumerable<Using> usings)
        {
            _usings.Clear();
            _usings.AddRange(usings);
        }

        public static readonly Func<string, Type> TestAppDomainAssemblies =
            n => AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(n)).Where(x => x != null).FirstOrDefault();

        public IEnumerable<string> EnumerateTestNames(string name)
        {
            yield return name;

            foreach (var @using in _usings)
            {
                string testName;

                if (@using.Alias != null && name.StartsWith(@using.Alias + "."))
                {
                    testName = @using.Namespace + name.Substring(@using.Alias.Length);
                }
                else
                {
                    testName = @using.Namespace + "." + name;
                }

                yield return testName;
            }
        }

        public Type TryResolveType(string name, Func<string, Type> test)
        {
            return EnumerateTestNames(name).Select(test).FirstOrDefault(type => type != null);
        }

        public IEnumerator<Using> GetEnumerator()
        {
            return _usings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _usings.GetEnumerator();
        }
    }
}