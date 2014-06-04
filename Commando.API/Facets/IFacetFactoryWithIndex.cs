using System;
using System.Collections.Generic;
using twomindseye.Commando.API1.Parse;

namespace twomindseye.Commando.API1.Facets
{
    public interface IFacetFactoryWithIndex : IFacetFactory
    {
        IEnumerable<ParseResult> Parse(ParseInput input, IEnumerable<FacetMoniker> indexEntries);
        IEnumerable<FacetMoniker> EnumerateIndex();
        FactoryIndexMode IndexMode { get; }
        bool ShouldUpdateIndex(FacetIndexReason indexReason, DateTime? lastUpdatedAt);
    }
}