using System;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.Facets
{
    public sealed class DateTimeFacet : Facet, IDateTimeFacet
    {
        readonly DateTime _value;
        readonly DateTimeFacetType _type;

        public DateTimeFacet(DateTime value, DateTimeFacetType type)
        {
            _value = value;
            _type = type;
        }

        public DateTimeFacetType Type
        {
            get { return _type; }
        }

        public DateTime Value
        {
            get { return _value; }
        }

        public override string DisplayName
        {
            get { return _value.ToString(); }
        }
    }
}
