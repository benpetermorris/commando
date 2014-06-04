using System;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using twomindseye.Commando.API1.Extension;

namespace twomindseye.Commando.UI.Util
{
    public sealed class ConfiguratorBindingProxy : DynamicObject, INotifyPropertyChanged, IDisposable
    {
        readonly IConfigurator _configurator;
        readonly ConfiguratorMetadata _metadata;
        readonly MarshalledPropertyChangedDelegator _propertyChanged;

        public ConfiguratorBindingProxy(IConfigurator configurator, ConfiguratorMetadata metadata)
        {
            _propertyChanged = new MarshalledPropertyChangedDelegator();
            _propertyChanged.PropertyChanged += HandleSerializedPropertyChanged;
            _configurator = configurator;
            _configurator.SerializablePropertyChanged += _propertyChanged.HandlePropertyChanged;
            _metadata = metadata ?? configurator.Metadata;
        }

        void HandleSerializedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var ev = PropertyChanged;

            if (ev != null)
            {
                ev(this, e);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!_metadata.Properties.Any(x => x.PropertyName == binder.Name))
            {
                result = null;
                return false;
            }

            result = _configurator.GetValue(binder.Name);

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (!_metadata.Properties.Any(x => x.PropertyName == binder.Name))
            {
                return false;
            }

            _configurator.SetValue(binder.Name, value);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            _configurator.SerializablePropertyChanged -= _propertyChanged.HandlePropertyChanged;
        }
    }
}
