using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using twomindseye.Commando.API1;

namespace twomindseye.Commando.Engine
{
    [Serializable]
    [DebuggerDisplay("{Name}")]
    sealed class ParameterDescriptor
    {
        public ParameterDescriptor(string name, TypeDescriptor parameterType, IEnumerable<APIAttribute> apiAttributes)
        {
            Name = name;
            ParameterType = parameterType;
            APIAttributes = new ReadOnlyCollection<APIAttribute>(apiAttributes.ToArray());
        }

        public string Name { get; private set; }
        public TypeDescriptor ParameterType { get; private set; }
        public ReadOnlyCollection<APIAttribute> APIAttributes { get; private set; }
    }
}