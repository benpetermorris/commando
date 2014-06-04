using System;
using System.Collections.Generic;
using System.Linq;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.API1.Facets
{
    public abstract class FacetFactory : ExtensionObject, IFacetFactory
    {
        public IEnumerable<ParseResult> Parse(ParseInput input, ParseMode mode, IList<TypeMoniker> facetTypes)
        {
            var types = facetTypes.Select(x => Type.GetType(x.AssemblyQualifiedName)).ToArray();
            return new MarshalledEnumerable<ParseResult>(ParseImpl(input, mode, types));
        }

        public TypeMoniker[] GetFacetTypes()
        {
            return GetFacetTypesImpl().Select(x => new TypeMoniker(x)).ToArray();
        }

        public abstract bool CanCreateFacet(FacetMoniker moniker);

        public abstract IFacet CreateFacet(FacetMoniker moniker);

        protected abstract Type[] GetFacetTypesImpl();

        protected abstract IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes);

        protected FacetMoniker CreateMonikerOf<TFacet>(string displayName, string factoryData)
            where TFacet : IFacet
        {
            return CreateMonikerOf<TFacet>(displayName, factoryData, null);
        }

        protected FacetMoniker CreateMonikerOf<TFacet>(string displayName, string factoryData, IEnumerable<FacetExtraData> extraData)
            where TFacet : IFacet
        {
            return new FacetMoniker(GetType(), typeof(TFacet), factoryData, displayName, extraData: extraData);
        }
    }
}
