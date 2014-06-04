using System.Collections.ObjectModel;
using System.Linq;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine.Load
{
    public class LoaderCommand : LoaderExtensionItem
    {
        ReadOnlyCollection<string> _aliases; 

        internal LoaderCommand(LoaderExtension extension, Command command) 
            : base(extension)
        {
            Command = command;
            _aliases = new ReadOnlyCollection<string>(Command.OriginalAliases);
        }

        public override LoaderConfiguratorType ConfiguratorType
        {
            get
            {
                //return Extension.GetConfiguratorFor(Command.Container.GetType());
                return null;
            }
        }

        internal override string DatabaseName
        {
            get
            {
                return Command.Name;
            }
        }

        public ReadOnlyCollection<string> Aliases
        {
            get
            {
                return _aliases;
            }
        }

        public Command Command { get; private set; }

        internal void SetAliases(string[] aliases)
        {
            _aliases = new ReadOnlyCollection<string>(aliases.ToArray());
            RaisePropertyChanged("Aliases");
        }
    }
}