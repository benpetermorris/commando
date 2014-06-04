using System;
using System.Collections.Generic;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;

namespace twomindseye.Commando.Engine
{
    internal sealed class EngineFacetsFactory : FacetFactory
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (TextFacet)};
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            return null;
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new TextFacet(moniker.FactoryData);
        }
    }
}