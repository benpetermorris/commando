using twomindseye.Commando.API1.Extension;

namespace twomindseye.Commando.API1.Facets
{
    public abstract class Facet : ExtensionObject, IFacet
    {
        /// <summary>
        /// The display name of the Facet.
        /// </summary>
        public abstract string DisplayName { get; }

        public virtual void Dispose()
        {
        }
    }
}
