using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1Impl.Facets;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Standard1Impl.Factories
{
    public sealed class DurationFactory : FacetFactory
    {
        static Regex s_regex = new Regex(@"\b(?'hours'\d+h)(?'mins'\d+m)?\b", RegexOptions.IgnoreCase);

        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (DurationFacet)};
        }

        static int ParseSegment(string segment)
        {
            return int.Parse(segment.TakeWhile(char.IsNumber).CharsToString());
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            return from term in input.Terms
                   from match in s_regex.Matches(term.Text).Cast<Match>()
                   where match.Success
                   let hg = match.Groups["hours"]
                   let mg = match.Groups["mins"]
                   let ts = new TimeSpan(hg.Success ? ParseSegment(hg.Value) : 0, mg.Success ? ParseSegment(mg.Value) : 0, 0)
                   let moniker = CreateMonikerOf<DurationFacet>(ts.ToString(), ts.ToString())
                   select new ParseResult(term, moniker, 1.0);
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            TimeSpan notused;
            return TimeSpan.TryParse(moniker.FactoryData, out notused);
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new DurationFacet(TimeSpan.Parse(moniker.FactoryData));
        }
    }
}
