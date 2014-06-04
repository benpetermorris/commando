using System;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.Facets
{
    public sealed class DurationFacet : Facet, IDurationFacet
    {
        readonly TimeSpan _duration;

        public DurationFacet(TimeSpan duration)
        {
            _duration = duration;
        }

        public override string DisplayName
        {
            get
            {
                return _duration.ToString();
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return _duration;
            }
        }
    }
}
