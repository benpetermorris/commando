using System;
using System.ComponentModel;

namespace twomindseye.Commando.API1.Extension
{
    public interface IConfigurator : IExtensionObject, INotifyPropertyChanged
    {
        ConfiguratorMetadata Metadata { get; }
        void LoadFromStore();
        void SaveToStore();
        void SetValue(string propertyName, object value);
        object GetValue(string propertyName);
        bool IsConfigured();
        event EventHandler<SerializablePropertyChangedEventArgs> SerializablePropertyChanged;
    }
}