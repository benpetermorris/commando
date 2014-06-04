using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Standard1.FacetTypes
{
    public interface ICalendarFacet : IFacet
    {
        void MakeAppointment(IContactFacet contact, IDateTimeFacet at, IDurationFacet duration, ITextFacet subject);
    }
}
