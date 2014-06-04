using System;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Google.Facets
{
    [AmbientFacet("E178ECC0-3026-4FCC-A45C-A6260295AAAA")]
    public sealed class CalendarFacet : Facet, ICalendarFacet
    {
        public override void Initialize(API1.Extension.IExtensionHooks extensionHooks)
        {
            base.Initialize(extensionHooks);

            //var store = extensionHooks.GetKeyValueStore();
            //store.RemoveValue("password");
        }

        public override string DisplayName
        {
            get
            {
                return "Google Calendar";
            }
        }

        public void MakeAppointment(IContactFacet contact, IDateTimeFacet at, IDurationFacet duration, ITextFacet subject)
        {
            if (contact == null || at == null)
            {
                return;
            }

            var store = ExtensionHooks.GetKeyValueStore();
            var username = store.GetString("username");
            var password = store.GetProtectedString("password");

            if (username == null || password == null)
            {
                throw new RequiresConfigurationException(typeof(Configurators.CredentialsConfigurator), typeof(CalendarFacet));
            }

            var cal = new CalendarService("Commando");
            cal.setUserCredentials(username, password);

            var finalDuration = duration != null ? duration.Duration : TimeSpan.FromHours(1);

            var e = new EventEntry(subject == null ? "Appointment" : subject.Text);
            e.Times.Add(new When(at.Value, at.Value.Add(finalDuration)));
            var who = new Who();
            who.Email = contact.Email;
            who.Rel = Who.RelType.EVENT_ATTENDEE;
            e.Participants.Add(who);

            try
            {
                cal.Insert(new Uri("https://www.google.com/calendar/feeds/default/private/full"), e);
            }
            catch (CaptchaRequiredException)
            {
                throw new InvalidOperationException(
                    "Google has locked the account. Please visit http://www.google.com/accounts/DisplayUnlockCaptcha to unlock it.");
            }
            catch (InvalidCredentialsException)
            {
                throw new RequiresConfigurationException(typeof(Configurators.CredentialsConfigurator), typeof(CalendarFacet), 
                                                         "The username and/or password are incorrect.");
            }
            catch (ClientFeedException ex)
            {
                throw new InvalidOperationException(string.Format("Could not add the appointment: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not add the appointment.", ex);
            }
        }
    }
}
