using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Google.Facets;

namespace twomindseye.Commando.Google.Configurators
{
    [Configures(typeof(CalendarFacet))]
    [ConfiguratorName("Google Credentials")]
    public class CredentialsConfigurator : Configurator
    {
        string _username;
        string _password;

        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                RaisePropertyChanged("Username");
            }
        }

        [ConfiguratorProperty(IsPassword = true)]
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                RaisePropertyChanged("Password");
            }
        }
    }
}
