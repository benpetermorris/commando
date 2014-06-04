using System;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Standard1.FacetTypes
{
    [Flags]
    public enum DateTimeFacetType
    {
        Date = 0x01,
        Time = 0x02,
        Both = Date | Time
    }

    [ExtraDataDefinition("Type", "Date", "Time", "DateTime")]
    public interface IDateTimeFacet : IFacet
    {
        DateTimeFacetType Type { get; }
        DateTime Value { get; }
    }
}
