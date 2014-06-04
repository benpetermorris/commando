using System;
using System.ComponentModel;

namespace twomindseye.Commando.API1.Extension
{
    /// <summary>
    /// Provides a basic implementation of IConfigurator. Use is optional; extension developers may
    /// implement IConfigurator directly.
    /// </summary>
    public abstract class Configurator : ExtensionObject, IConfigurator
    {
        readonly Lazy<ConfiguratorMetadata> _metadata;

        protected Configurator()
        {
            _metadata = new Lazy<ConfiguratorMetadata>(() => new ConfiguratorMetadata(GetType()));
        }

        public ConfiguratorMetadata Metadata
        {
            get
            {
                return _metadata.Value;
            }
        }

        protected virtual void OnBeforeLoad()
        {
        }

        protected virtual void OnLoadFromStore()
        {
            var store = ExtensionHooks.GetKeyValueStore();

            foreach (var property in _metadata.Value.Properties)
            {
                if (property.IsPassword)
                {
                    SetValue(property.PropertyName, store.GetProtectedString(property.StoreKey));
                }
                else
                {
                    SetValue(property.PropertyName, store[property.StoreKey]);
                }
            }
        }

        protected virtual void OnAfterLoad()
        {
        }

        protected virtual void OnBeforeSave()
        {
        }

        protected virtual void OnSaveToStore()
        {
            var store = ExtensionHooks.GetKeyValueStore();

            foreach (var property in _metadata.Value.Properties)
            {
                if (property.IsPassword)
                {
                    store.SetProtectedString(property.StoreKey, (string)GetValue(property.PropertyName));
                }
                else
                {
                    store[property.StoreKey] = GetValue(property.PropertyName);
                }
            }
        }

        protected virtual void OnAfterSave()
        {
        }

        public void LoadFromStore()
        {
            OnBeforeLoad();
            OnLoadFromStore();
            OnAfterLoad();
        }

        public void SaveToStore()
        {
            OnBeforeSave();
            OnSaveToStore();
            OnAfterSave();
        }

        public void SetValue(string propertyName, object value)
        {
            GetType().GetProperty(propertyName).SetValue(this, value, null);
        }

        public object GetValue(string propertyName)
        {
            return GetType().GetProperty(propertyName).GetValue(this, null);
        }

        public virtual bool IsConfigured()
        {
            var store = ExtensionHooks.GetKeyValueStore();

            foreach (var property in _metadata.Value.Properties)
            {
                if (store[property.StoreKey] == null)
                {
                    return false;
                }
            }

            return true;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            var e = PropertyChanged;

            if (e != null)
            {
                e(this, new PropertyChangedEventArgs(propertyName));
            }

            var e2 = SerializablePropertyChanged;

            if (e2 != null)
            {
                e2(this, new SerializablePropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<SerializablePropertyChangedEventArgs> SerializablePropertyChanged;
    }
}
