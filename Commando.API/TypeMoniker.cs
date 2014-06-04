using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace twomindseye.Commando.API1
{
    /// <summary>
    /// A lightweight, serializable Type identifier that places no type-loading burden on the receiving 
    /// application domain.
    /// </summary>
    [Serializable]
    public sealed class TypeMoniker : IEquatable<TypeMoniker>
    {
        // lenient: we only really care about the major structure
        static readonly Regex s_aqnRegex = new Regex(@"^(.+?), ([^\<\>\:""/\\\|\?\*,]+), Version=\d+\.\d+\.\d+\.\d+, Culture=\S+, PublicKeyToken=(null|[a-f0-9]{16})$", RegexOptions.IgnoreCase);

        [NonSerialized] 
        string _fullName;
        [NonSerialized]
        string _name;
        [NonSerialized]
        string _assemblyName;

        public TypeMoniker(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            AssemblyQualifiedName = type.AssemblyQualifiedName;
        }

        public TypeMoniker(string typeAQN)
        {
            if (typeAQN == null)
            {
                throw new ArgumentNullException("typeAQN");
            }

            if (!s_aqnRegex.IsMatch(typeAQN))
            {
                throw new ArgumentException("Not a valid AssemblyQualifiedName", "typeAQN");
            }

            AssemblyQualifiedName = typeAQN;
        }

        void Init()
        {
            var match = s_aqnRegex.Match(AssemblyQualifiedName);

            if (!match.Success)
            {
                throw new InvalidOperationException("Invalid AssemblyQualifiedName");
            }

            _fullName = match.Groups[1].Value;
            _assemblyName = match.Groups[2].Value;

            _name = _fullName;

            if (_fullName.Contains("["))
            {
                // constructed generic type - get rid of arg list
                _name = _name.Substring(0, _name.IndexOf('['));
            }

            _name = _name.Substring(Math.Max(_name.LastIndexOf('.'), -1) + 1);
        }

        public static bool IsValidAssemblyQualifiedName(string aqn)
        {
            return s_aqnRegex.IsMatch(aqn);
        }

        public string AssemblyQualifiedName { get; private set; }

        public string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    Init();
                }

                return _fullName;
            }
        }

        public string Name
        {
            get
            {
                if (_fullName == null)
                {
                    Init();
                }

                return _name;
            }
        }

        public string AssemblyName
        {
            get
            {
                if (_fullName == null)
                {
                    Init();
                }

                return _assemblyName;
            }
        }

        public AssemblyName GetAssemblyName()
        {
            return new AssemblyName(AssemblyName);
        }

        public static implicit operator TypeMoniker(Type type)
        {
            return new TypeMoniker(type);
        }

        public override string ToString()
        {
            return AssemblyQualifiedName;
        }

        public bool Equals(TypeMoniker other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return AssemblyQualifiedName == other.AssemblyQualifiedName;
        }

        public bool Equals(Type other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return AssemblyQualifiedName == other.AssemblyQualifiedName;
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

            if (obj is TypeMoniker)
            {
                return Equals((TypeMoniker) obj);
            }

            if (obj is Type)
            {
                return Equals((Type) obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return AssemblyQualifiedName.GetHashCode();
        }

        public static bool operator ==(TypeMoniker left, TypeMoniker right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypeMoniker left, TypeMoniker right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(TypeMoniker left, Type right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypeMoniker left, Type right)
        {
            return !Equals(left, right);
        }
    }
}
