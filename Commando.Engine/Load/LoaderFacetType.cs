using System;
using System.Diagnostics;
using twomindseye.Commando.API1;

namespace twomindseye.Commando.Engine.Load
{
    [DebuggerDisplay("{DatabaseName}")]
    public sealed class LoaderFacetType : LoaderExtensionItem
    {
        internal LoaderFacetType(LoaderExtension extension, TypeDescriptor facetType)
            : base(extension)
        {
            Type = facetType;
            TypeDescriptor = facetType;
        }

        public TypeMoniker Type { get; private set; }

        public override LoaderConfiguratorType ConfiguratorType
        {
            get
            {
                return Extension.GetConfiguratorFor(Type);
            }
        }

        internal TypeDescriptor TypeDescriptor { get; private set; }

        internal override string DatabaseName
        {
            get
            {
                return Type.FullName;
            }
        }
    }
}