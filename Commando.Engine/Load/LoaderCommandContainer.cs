using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Commands;

namespace twomindseye.Commando.Engine.Load
{
    public class LoaderCommandContainer : LoaderExtensionItem
    {
        internal LoaderCommandContainer(LoaderExtension extension, CommandContainer container, TypeMoniker containerType)
            : base(extension)
        {
            Container = container;
            ContainerType = containerType;
        }

        public override LoaderConfiguratorType ConfiguratorType
        {
            get
            {
                return Extension.GetConfiguratorFor(ContainerType);
            }
        }

        internal override string DatabaseName
        {
            get
            {
                return ContainerType.FullName;
            }
        }

        internal CommandContainer Container { get; private set; }
        public TypeMoniker ContainerType { get; private set; }
    }
}