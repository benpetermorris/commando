using System;
using System.Collections.Generic;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.API1.Facets
{
    public abstract class FacetFactoryWithIndex : FacetFactory, IFacetFactoryWithIndex
    {
        protected sealed override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ParseResult> Parse(ParseInput input, IEnumerable<FacetMoniker> indexEntries)
        {
            return new MarshalledEnumerable<ParseResult>(ParseImpl(input, indexEntries));
        }

        protected abstract IEnumerable<ParseResult> ParseImpl(ParseInput input, IEnumerable<FacetMoniker> indexEntries);

        public IEnumerable<FacetMoniker> EnumerateIndex()
        {
            return new MarshalledEnumerable<FacetMoniker>(EnumerateIndexImpl());
        }

        protected abstract IEnumerable<FacetMoniker> EnumerateIndexImpl();

        public abstract FactoryIndexMode IndexMode { get; }

        public abstract bool ShouldUpdateIndex(FacetIndexReason indexReason, DateTime? lastUpdatedAt);
    }
}
