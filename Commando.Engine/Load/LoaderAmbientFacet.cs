using System;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Engine.Load
{
    public class LoaderAmbientFacet : LoaderExtensionItem
    {
        internal LoaderAmbientFacet(LoaderExtension extension, Guid ambientToken, IFacet facet, FacetMoniker moniker) 
            : base(extension)
        {
            AmbientToken = ambientToken;
            IFacet = facet;
            Moniker = moniker;
        }

        public override LoaderConfiguratorType ConfiguratorType
        {
            get
            {
                return Extension.GetConfiguratorFor(Moniker.FacetType);
            }
        }

        internal override string DatabaseName
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        internal Guid AmbientToken { get; private set; }
        internal FacetMoniker Moniker { get; private set; }
        internal IFacet IFacet { get; private set; }
    }
}