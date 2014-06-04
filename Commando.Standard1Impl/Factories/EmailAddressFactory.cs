using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Standard1Impl.Factories
{
    public sealed class EmailAddressFactory : FacetFactory
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (EmailAddressFacet)};
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            var regex = new Regex(@"[\w\.]+\@\w+(\.\w+)+");

            return (from m in regex.Matches(input.Text).Cast<Match>()
                    let range = new ParseRange(m.Index, m.Length)
                    let moniker = new FacetMoniker(GetType(), typeof(EmailAddressFacet), m.Value, m.Value, null)
                    select new ParseResult(input, range, moniker, ParseResult.ExactMatchRelevance)).ToArray();
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new EmailAddressFacet(moniker.DisplayName);
        }
    }
}
