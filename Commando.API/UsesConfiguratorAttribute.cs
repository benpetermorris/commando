using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace twomindseye.Commando.API1
{
    [AttributeUsage(AttributeTargets.Class)]
    [Serializable]
    public sealed class UsesConfiguratorAttribute : APIAttribute
    {
        public UsesConfiguratorAttribute(Type configuratorType)
        {
            ConfiguratorType = configuratorType;
        }

        public TypeMoniker ConfiguratorType { get; private set; }
    }
}
