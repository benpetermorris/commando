using System;
using System.Collections.Generic;
using System.Linq;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Standard1Impl.Factories
{
    public sealed class UrlFacetFactory : FacetFactory
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (UrlFacet)};
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            Uri uri;

            return from term in input
                   where term.Text.Contains("://")
                   where Uri.TryCreate(term.Text, UriKind.Absolute, out uri) 
                   select new ParseResult(term, CreateMonikerOf<UrlFacet>(term.Text, term.Text), 1.0);
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new UrlFacet(moniker.FactoryData);
        }
    }
}
