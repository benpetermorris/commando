using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace twomindseye.Commando.API1.Extension
{
    /// <summary>
    /// Holds information about a single user-configurable code property exposed by a Configurator.
    /// </summary>
    [Serializable]
    public sealed class ConfiguratorPropertyMetadata
    {
        readonly string _propertyName;
        readonly string _choicesPropertyName;
        readonly string _enabledPropertyName;
        readonly string _visiblePropertyName;
        readonly ConfiguratorPropertyAttribute _attr;

        internal ConfiguratorPropertyMetadata(ConfiguratorMetadata metadata, PropertyInfo property, List<PropertyInfo> relatedProperties)
        {
            Metadata = metadata;
            _propertyName = property.Name;

            _choicesPropertyName = relatedProperties.Where(ConfiguratorMetadata.IsChoicesProperty).Select(x => x.Name).FirstOrDefault();
            _enabledPropertyName = relatedProperties.Where(ConfiguratorMetadata.IsEnabledProperty).Select(x => x.Name).FirstOrDefault();
            _visiblePropertyName = relatedProperties.Where(ConfiguratorMetadata.IsVisibleProperty).Select(x => x.Name).FirstOrDefault();

            _attr = property
                .GetCustomAttributes(typeof (ConfiguratorPropertyAttribute), false)
                .Cast<ConfiguratorPropertyAttribute>()
                .SingleOrDefault();
        }

        public ConfiguratorMetadata Metadata { get; private set; }

        public string DisplayName
        {
            get
            {
                return (_attr == null ? PropertyName : _attr.DisplayName) ?? PropertyName;
            }
        }

        public string GroupName
        {
            get
            {
                return _attr == null ? null : _attr.GroupName;
            }
        }

        public string StoreKey
        {
            get
            {
                return (_attr == null ? null : _attr.StoreKey) ?? _propertyName;
            }
        }

        public bool AllowUserValue
        {
            get
            {
                return _attr == null ? false : _attr.AllowUserValue;
            }
        }

        public bool IsPassword
        {
            get
            {
                return _attr != null && _attr.IsPassword;
            }
        }

        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        public string ChoicesPropertyName
        {
            get
            {
                return _choicesPropertyName;
            }
        }

        public string EnabledPropertyName
        {
            get
            {
                return _enabledPropertyName;
            }
        }

        public string VisiblePropertyName
        {
            get
            {
                return _visiblePropertyName;
            }
        }

        public bool HasChoices
        {
            get
            {
                return _choicesPropertyName != null;
            }
        }
    }
}