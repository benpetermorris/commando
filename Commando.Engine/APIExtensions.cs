using System;
using System.Linq;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Engine.Load;

namespace twomindseye.Commando.Engine
{
    internal static class APIExtensions
    {
        public static LoaderFacetFactory GetFactory(this FacetMoniker moniker)
        {
            if (moniker.IsAmbient)
            {
                return null;
            }

            return Loader.FacetFactories
                .Where(x => x.Type == moniker.FactoryType)
                .SingleOrDefault();
        }

        public static IFacet CreateFacet(this FacetMoniker moniker)
        {
            if (moniker.IsAmbient)
            {
                return Loader.AmbientFacets
                    .Where(x => x.AmbientToken == moniker.AmbientToken.Value)
                    .Select(x => x.IFacet)
                    .SingleOrDefault();
            }

            var factory = moniker.GetFactory();

            return factory == null ? null : factory.CreateFacet(moniker);
        }

        public static bool IsFactorySourceValid(this ParseInputTerm term, LoaderFacetFactory f)
        {
            return term.Mode != TermParseMode.SpecificFactory ||
                   (term.Mode == TermParseMode.SpecificFactory &&
                    f.Aliases.Contains(term.FactoryAlias, StringComparer.CurrentCultureIgnoreCase));
        }

        public static ParseInput RewriteInput(this ParseInput input, LoaderFacetFactory forFactory, bool fromHistory)
        {
            if (!input.Any(x => x.Mode != TermParseMode.Normal))
            {
                return input;
            }

            return new ParseInput(input.Where(x => 
                (x.Mode == TermParseMode.Normal || 
                 (x.Mode == TermParseMode.History && fromHistory)) && 
                x.IsFactorySourceValid(forFactory)));
        }
    }
}
