using System;
using System.Collections.Generic;

namespace twomindseye.Commando.API1.Facets
{
    [Serializable]
    public sealed class FacetExtraData
    {
        public FacetExtraData(TypeMoniker facetType, string key, string value)
        {
            FacetType = facetType;
            Key = key;
            Value = value;
        }

        public TypeMoniker FacetType { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }

        public static IEnumerable<FacetExtraData> BeginWith(TypeMoniker facetType, string key, string value)
        {
            yield return new FacetExtraData(facetType, key, value);
        }

        public static IEnumerable<FacetExtraData> BeginWith<TFacet>(string key, string value)
        {
            yield return new FacetExtraData(typeof(TFacet), key, value);
        }

        public override string ToString()
        {
            return string.Format("{0}={1}={2}", FacetType.Name, Key, Value);
        }
    }
}