using System;
using System.Collections.Generic;

namespace twomindseye.Commando.API1.Facets
{
    public static class FacetExtraDataExtensions
    {
        public static IEnumerable<FacetExtraData> Append(this IEnumerable<FacetExtraData> sequence, TypeMoniker facetType, string key, string value)
        {
            foreach (var item in sequence)
            {
                yield return item;
            }

            yield return new FacetExtraData(facetType, key, value);
        }

        public static IEnumerable<FacetExtraData> Append<TFacet>(this IEnumerable<FacetExtraData> sequence, string key, string value)
        {
            foreach (var item in sequence)
            {
                yield return item;
            }

            yield return new FacetExtraData(typeof(TFacet), key, value);
        }
    }
}