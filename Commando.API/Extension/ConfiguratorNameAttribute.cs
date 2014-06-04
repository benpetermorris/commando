using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace twomindseye.Commando.API1.Extension
{
    [AttributeUsage(AttributeTargets.Class)]
    [Serializable]
    public sealed class ConfiguratorNameAttribute : APIAttribute
    {
        public ConfiguratorNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
