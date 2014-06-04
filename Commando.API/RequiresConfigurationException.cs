using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace twomindseye.Commando.API1
{
    [Serializable]
    public sealed class RequiresConfigurationException : Exception
    {
        protected RequiresConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Description = info.GetString("Description");
            ConfiguratorType = (TypeMoniker)info.GetValue("ConfiguratorType", typeof(TypeMoniker));
            TypeRequiringConfiguration = (TypeMoniker)info.GetValue("TypeRequiringConfiguration", typeof(TypeMoniker));
        }

        public RequiresConfigurationException(Type configuratorType, Type typeRequiringConfiguration, string description = null)
        {
            if (configuratorType == null)
            {
                throw new ArgumentNullException("configuratorType");
            }

            if (typeRequiringConfiguration == null)
            {
                throw new ArgumentNullException("typeRequiringConfiguration");
            }

            ConfiguratorType = configuratorType;
            TypeRequiringConfiguration = typeRequiringConfiguration;
            Description = description;
        }

        public string Description { get; private set; }
        public TypeMoniker ConfiguratorType { get; private set; }
        public TypeMoniker TypeRequiringConfiguration { get; private set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Description", Description);
            info.AddValue("ConfiguratorType", ConfiguratorType);
            info.AddValue("TypeRequiringConfiguration", TypeRequiringConfiguration);
        }
    }
}
