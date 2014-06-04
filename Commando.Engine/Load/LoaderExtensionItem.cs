namespace twomindseye.Commando.Engine.Load
{
    public abstract class LoaderExtensionItem : LoaderItem
    {
        protected LoaderExtensionItem(LoaderExtension extension)
        {
            Extension = extension;
        }

        public virtual LoaderConfiguratorType ConfiguratorType
        {
            get
            {
                return null;
            }
        }

        public LoaderExtension Extension { get; private set; }
        
        internal abstract string DatabaseName { get; }

        internal virtual void OnLoadComplete()
        {
        }
    }
}