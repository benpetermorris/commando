using System;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine.Load
{
    public sealed class LoaderConfiguratorType : LoaderExtensionItem
    {
        readonly Lazy<ConfiguratorMetadata> _metadata;

        public LoaderConfiguratorType(LoaderExtension extension, TypeMoniker type)
            : base(extension)
        {
            _metadata = new Lazy<ConfiguratorMetadata>(GetMetadata);
            Type = type;
        }

        public TypeMoniker Type { get; private set; }

        public ConfiguratorMetadata Metadata
        {
            get
            {
                return _metadata.Value;
            }
        }

        public IConfigurator Create()
        {
            var lae = (LoaderAssemblyExtension) Extension;
            var rvl = lae.CreateLoaderInstance<IConfigurator>(Type);
            rvl.Initialize(Extension.Hooks);
            return rvl;
        }

        ConfiguratorMetadata GetMetadata()
        {
            var configurator = Create();
            var rvl = configurator.Metadata;
            var lae = (LoaderAssemblyExtension)Extension;
            lae.UnregisterLease(configurator);
            return rvl;
        }

        internal override string DatabaseName
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}