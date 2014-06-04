using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace twomindseye.Commando.API1.Extension
{
    [AttributeUsage(AttributeTargets.Class)]
    [Serializable]
    public sealed class ConfiguresAttribute : APIAttribute
    {
        public ConfiguresAttribute(params Type[] configuresTypes)
        {
            ConfiguresTypes = configuresTypes.Select(x => new TypeMoniker(x)).ToArray();
        }

        public TypeMoniker[] ConfiguresTypes { get; private set; }
    }
}
