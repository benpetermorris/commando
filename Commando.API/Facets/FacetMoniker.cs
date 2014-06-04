using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.API1.Facets
{
    /// <summary>
    /// Represents a potential Facet.
    /// </summary>
    [Serializable]
    public sealed class FacetMoniker : IEquatable<FacetMoniker>
    {
        static readonly FacetExtraData[] s_emptyExtraData = new FacetExtraData[0];
        
        readonly FacetExtraData[] _extraData;

        [NonSerialized]
        ReadOnlyCollection<FacetExtraData> _extraDataCollection;
        
        [NonSerialized]
        string _hashString;

        /// <summary>
        /// Constructor to represent normal facets.
        /// </summary>
        /// <param name="factoryType"></param>
        /// <param name="facetType"></param>
        /// <param name="factoryData"></param>
        /// <param name="displayName"></param>
        /// <param name="dateTime"></param>
        /// <param name="sourceName"></param>
        /// <param name="iconPath"></param>
        /// <param name="extraData"></param>
        public FacetMoniker(TypeMoniker factoryType, TypeMoniker facetType, string factoryData, string displayName, DateTime? dateTime = null, string sourceName = null,
            string iconPath = null, IEnumerable<FacetExtraData> extraData = null)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            if (factoryType == null)
            {
                throw new ArgumentNullException("factoryType");
            }

            if (facetType == null)
            {
                throw new ArgumentNullException("facetType");
            }

            if (factoryData == null)
            {
                throw new ArgumentNullException("factoryData");
            }

            DisplayName = displayName;
            FactoryType = factoryType;
            FacetType = facetType;
            FactoryData = factoryData;
            Source = sourceName;
            IconPath = iconPath;
            DateTime = dateTime;

            _extraData = extraData == null ? s_emptyExtraData : extraData.ToArray();
        }

        /// <summary>
        /// Constructor to represent ambient facets.
        /// </summary>
        /// <param name="ambientToken"></param>
        /// <param name="facetType"></param>
        /// <param name="displayName"></param>
        /// <param name="iconPath"></param>
        /// <param name="extraData"></param>
        public FacetMoniker(Guid ambientToken, TypeMoniker facetType, string displayName, string iconPath = null, IEnumerable<FacetExtraData> extraData = null)
        {
            if (facetType == null)
            {
                throw new ArgumentNullException("facetType");
            }

            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            AmbientToken = ambientToken;
            FacetType = facetType;
            DisplayName = displayName;
            IconPath = iconPath;

            _extraData = extraData == null ? s_emptyExtraData : extraData.ToArray();
        }

        public string DisplayName { get; private set; }
        
        public string Source { get; private set; }
        
        public string IconPath { get; private set; }

        public DateTime? DateTime { get; private set; }

        public TypeMoniker FactoryType { get; private set; }

        public Guid? AmbientToken { get; private set; }

        public TypeMoniker FacetType { get; private set; }

        public string FactoryData { get; private set; }

        public bool IsAmbient
        {
            get
            {
                return AmbientToken.HasValue;
            }
        }

        public ReadOnlyCollection<FacetExtraData> ExtraData
        {
            get
            {
                return _extraDataCollection ??
                       (_extraDataCollection = new ReadOnlyCollection<FacetExtraData>(_extraData));
            }
        }

        public override string ToString()
        {
            if (IsAmbient)
            {
                return string.Format("\"{0}\" [ambient {1}]", DisplayName, FacetType.Name);
            }

            return string.Format("\"{0}\" [{1} from {2}]", DisplayName, FacetType.Name, Source ?? "text");
        }

        public string this[string facetAQN, string key]
        {
            get
            {
                return _extraData
                    .Where(x => x.FacetType.AssemblyQualifiedName == facetAQN)
                    .Where(x => x.Key == key)
                    .Select(x => x.Value)
                    .FirstOrDefault();
            }
        }

        public bool Equals(FacetMoniker other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (IsAmbient)
            {
                return other.AmbientToken == AmbientToken;
            }

            return Equals(other.FactoryType, FactoryType) && Equals(other.FacetType, FacetType) && Equals(other.FactoryData, FactoryData);
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

            if (obj.GetType() != typeof(FacetMoniker))
            {
                return false;
            }

            return Equals((FacetMoniker)obj);
        }

        public string HashString
        {
            get
            {
                if (_hashString == null)
                {
                    var sb = new StringBuilder();

                    if (IsAmbient)
                    {
                        sb.Append(AmbientToken);
                    }
                    else
                    {
                        sb.Append(FactoryData);
                    }

                    if (_extraData != null)
                    {
                        foreach (var ed in _extraData)
                        {
                            sb.Append(ed);
                        }
                    }

                    using (var md5 = new MD5CryptoServiceProvider())
                    {
                        _hashString = md5.ComputeHash(Encoding.Unicode.GetBytes(sb.ToString())).ToHexString();
                    }
                }

                return _hashString;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (IsAmbient)
                {
                    return AmbientToken.GetHashCode();
                }

                var result = FactoryType.GetHashCode();
                result = (result * 397) ^ FacetType.GetHashCode();
                result = (result * 397) ^ FactoryData.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(FacetMoniker left, FacetMoniker right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FacetMoniker left, FacetMoniker right)
        {
            return !Equals(left, right);
        }
    }
}
