using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Standard1.FacetTypes
{
    public interface IDurationFacet : IFacet
    {
        TimeSpan Duration { get; }
    }
}
