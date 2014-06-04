using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using twomindseye.Commando.API1;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine
{
    [Serializable]
    [DebuggerDisplay("{FullName}")]
    sealed class TypeDescriptor : IEquatable<TypeDescriptor>, IDeserializationCallback
    {
        static readonly ConcurrentDictionary<string, TypeDescriptor> s_descriptors;

        static public readonly IEqualityComparer<AssemblyName> AssemblyNameComparer =
            DelegatedEqualityComparer.Create<AssemblyName>((a, b) => a.FullName == b.FullName);

        static TypeDescriptor()
        {
            s_descriptors = new ConcurrentDictionary<string, TypeDescriptor>();
        }

        public static TypeDescriptor Get(string aqn)
        {
            return GetImpl(aqn, true);
        }

        public static TypeDescriptor Get(TypeMoniker moniker)
        {
            return GetImpl(moniker.AssemblyQualifiedName, true);
        }

        public static TypeDescriptor TryGet(string fullName, AssemblyName assemblyName)
        {
            return TryGet(string.Format("{0}, {1}", fullName, assemblyName.FullName));
        }

        public static TypeDescriptor TryGet(string aqn)
        {
            return GetImpl(aqn, false);
        }

        public static TypeDescriptor TryGet(TypeMoniker moniker)
        {
            return GetImpl(moniker.AssemblyQualifiedName, false);
        }

        static TypeDescriptor GetImpl(string aqn, bool throwOnFail)
        {
            TypeDescriptor rvl;

            if (!s_descriptors.TryGetValue(aqn, out rvl) && throwOnFail)
            {
                throw new InvalidOperationException("No TypeDescriptor available for " + aqn);
            }

            return rvl;
        }

        public static TypeDescriptor GetOrAdd(Type type)
        {
            TypeDescriptor rvl;

            if (!s_descriptors.TryGetValue(type.AssemblyQualifiedName, out rvl))
            {
                s_descriptors.TryAdd(type.AssemblyQualifiedName, rvl = new TypeDescriptor(type));
            }

            return rvl;
        }

        public static TypeDescriptor[] Get(AssemblyName assembly)
        {
            return s_descriptors.Values
                .Where(x => x.AssemblyName.FullName == assembly.FullName)
                .ToArray();
        }

        public static void UnloadTypesFrom(AssemblyName assembly)
        {
            TypeDescriptor notUsed;
            foreach (var d in Get(assembly))
            {
                s_descriptors.TryRemove(d.AssemblyQualifiedName, out notUsed);
            }
        }

        public static AssemblyName[] GetAssemblyNames()
        {
            return s_descriptors.Values
                .Select(x => x.AssemblyName)
                .Distinct(AssemblyNameComparer)
                .ToArray();
        }

        readonly List<TypeDescriptor> _implements;

        private TypeDescriptor(Type type)
        {
            AssemblyQualifiedName = type.AssemblyQualifiedName;
            FullName = type.FullName;
            Name = type.Name;
            AssemblyName = type.Assembly.GetName();
            IsAbstract = type.IsAbstract;
            IsInterface = type.IsInterface;
            Moniker = new TypeMoniker(AssemblyQualifiedName);

            if (!IsAbstract && !IsInterface)
            {
                HasPublicParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null;
            }

            _implements = new List<TypeDescriptor>();

            if (type.BaseType != null)
            {
                _implements.Add(GetOrAdd(type.BaseType));
            }

            if (type.FullName.Contains("ITextFacet"))
            {
                int x = 10;
            }

            _implements.AddRange(type.GetInterfaces().Select(GetOrAdd));
        }

        public bool IsAbstract { get; private set; }
        public bool IsInterface { get; private set; }
        public bool HasPublicParameterlessConstructor { get; private set; }
        public string AssemblyQualifiedName { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public AssemblyName AssemblyName { get; private set; }
        public TypeMoniker Moniker { get; private set; }

        public bool Implements(TypeDescriptor descriptor)
        {
            return Implements(descriptor.AssemblyQualifiedName);
        }

        public bool Implements(TypeMoniker moniker)
        {
            return Implements(moniker.AssemblyQualifiedName);
        }

        public bool Implements(Type type)
        {
            return Implements(type.AssemblyQualifiedName);
        }

        bool Implements(string aqn)
        {
            return _implements.Any(x => x.ImplementsInternal(aqn));
        }

        private bool ImplementsInternal(string aqn)
        {
            return AssemblyQualifiedName == aqn || _implements.Any(x => x.Implements(aqn));
        }

        public IEnumerable<TypeDescriptor> GetImplementedTypes(bool recurse = false)
        {
            foreach (var i in _implements)
            {
                yield return i;

                if (recurse)
                {
                    foreach (var i2 in i.GetImplementedTypes(true))
                    {
                        yield return i2;
                    }
                }
            }
        }

        public static implicit operator TypeMoniker(TypeDescriptor descriptor)
        {
            return descriptor.Moniker;
        }

        public bool Equals(TypeDescriptor other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.AssemblyQualifiedName, AssemblyQualifiedName);
        }

        public bool Equals(TypeMoniker other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return Equals(other.AssemblyQualifiedName, AssemblyQualifiedName);
        }

        public bool Equals(Type other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return Equals(other.AssemblyQualifiedName, AssemblyQualifiedName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is TypeDescriptor)
            {
                return Equals((TypeDescriptor)obj);
            }
            if (obj is TypeMoniker)
            {
                return Equals((TypeMoniker)obj);
            }
            if (obj is Type)
            {
                return Equals((Type)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return AssemblyQualifiedName.GetHashCode();
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            // If the descriptor already exists in the target AppDomain, ensure it's
            // exactly the same as the deserialized. Not sure what the remedy is if not...

            TypeDescriptor ext;

            if (s_descriptors.TryGetValue(AssemblyQualifiedName, out ext))
            {
                if (IsAbstract != ext.IsAbstract ||
                    IsInterface != ext.IsInterface ||
                    HasPublicParameterlessConstructor != ext.HasPublicParameterlessConstructor ||
                    _implements.Count != ext._implements.Count ||
                    _implements.Except(ext._implements).Any())
                {
                    throw new InvalidOperationException("Deserialized TypeDescriptor does not match existing");
                }

                return;
            }

            s_descriptors.TryAdd(AssemblyQualifiedName, this);
        }

        public static bool operator ==(TypeDescriptor left, TypeDescriptor right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypeDescriptor left, TypeDescriptor right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(TypeDescriptor left, TypeMoniker right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypeDescriptor left, TypeMoniker right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
