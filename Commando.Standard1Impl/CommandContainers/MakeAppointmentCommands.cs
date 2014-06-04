using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.CommandContainers
{
    public sealed class MakeAppointmentCommands : CommandContainer
    {
        [Command("Make Appointment", Aliases = "appt")]
        public void MakeAppointment(
            [CommandParameter("Calendar")] ICalendarFacet calendar,
            [CommandParameter("With")] IContactFacet contact,
            [CommandParameter("Time")] IDateTimeFacet time,
            [CommandParameter("Duration", Optional = true)] IDurationFacet duration,
            [CommandParameter("Subject", Optional = true)] ITextFacet subject)
        {
            calendar.MakeAppointment(contact, time, duration, subject);
        }
    }
}
