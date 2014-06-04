using System.Collections.Generic;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.API1.Parse;

namespace twomindseye.Commando.API1.Facets
{
    public interface IFacetFactory : IExtensionObject
    {
        IEnumerable<ParseResult> Parse(ParseInput input, ParseMode mode, IList<TypeMoniker> facetTypes);
        TypeMoniker[] GetFacetTypes();
        bool CanCreateFacet(FacetMoniker moniker);
        IFacet CreateFacet(FacetMoniker moniker);
    }
}