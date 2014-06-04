using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.API1.Extension
{
    /// <summary>
    /// Holds information about the user-configurable code properties exposed by a Configurator.
    /// </summary>
    [Serializable]
    public sealed class ConfiguratorMetadata
    {
        readonly List<ConfiguratorPropertyMetadata> _properties;
        ConfiguresAttribute _configuresAttribute;
        ConfiguratorNameAttribute _configuratorNameAttribute;

        public ConfiguratorMetadata(Type configuratorType)
        {
            _properties = new List<ConfiguratorPropertyMetadata>();
            ConfiguratorType = configuratorType;
            Initialize(configuratorType);
        }

        public TypeMoniker ConfiguratorType { get; private set; }

        public bool ConfiguresType(TypeMoniker type)
        {
            if (type.AssemblyName != ConfiguratorType.AssemblyName)
            {
                throw new InvalidOperationException("Type is not in the same assembly as ConfiguratorType");
            }

            return _configuresAttribute == null || _configuresAttribute.ConfiguresTypes.Contains(type);
        }

        public IEnumerable<ConfiguratorPropertyMetadata> Properties
        {
            get
            {
                return _properties.AsEnumerable();
            }
        }

        public string Name
        {
            get
            {
                return _configuratorNameAttribute == null
                           ? ConfiguratorType.Name
                           : _configuratorNameAttribute.Name ?? ConfiguratorType.Name;
            }
        }

        void Initialize(Type configuratorType)
        {
            var allProperties = configuratorType.GetProperties().Where(x => x.GetSetMethod() != null);

            var keyProperties = allProperties
                .Where(x => !IsChoicesProperty(x) && !IsEnabledProperty(x) && !IsVisibleProperty(x))
                .ToArray();

            _configuresAttribute = configuratorType.GetCustomAttribute<ConfiguresAttribute>();
            _configuratorNameAttribute = configuratorType.GetCustomAttribute<ConfiguratorNameAttribute>();

            foreach (var property in keyProperties)
            {
                var relatedProperties = allProperties
                    .Where(x => x != property && x.Name.StartsWith(property.Name))
                    .ToList();

                _properties.Add(new ConfiguratorPropertyMetadata(this, property, relatedProperties));
            }
        }

        internal static bool IsChoicesProperty(PropertyInfo property)
        {
            return property.Name.EndsWith("PropertyChoices") && typeof(IEnumerable).IsAssignableFrom(property.PropertyType);
        }

        internal static bool IsEnabledProperty(PropertyInfo property)
        {
            return property.Name.EndsWith("PropertyEnabled") && property.PropertyType == typeof (bool);
        }

        internal static bool IsVisibleProperty(PropertyInfo property)
        {
            return property.Name.EndsWith("PropertyVisible") && property.PropertyType == typeof (bool);
        }
    }
}
