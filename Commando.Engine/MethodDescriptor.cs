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
    sealed class MethodDescriptor
    {
        public MethodDescriptor(TypeDescriptor declaringType, string name, IEnumerable<ParameterDescriptor> parameters, IEnumerable<APIAttribute> apiAttributes)
        {
            DeclaringType = declaringType;
            Name = name;
            Parameters = new ReadOnlyCollection<ParameterDescriptor>(parameters.ToArray());
            APIAttributes = new ReadOnlyCollection<APIAttribute>(apiAttributes.ToArray());
        }

        public TypeDescriptor DeclaringType { get; private set; }
        public string Name { get; private set; }
        public ReadOnlyCollection<ParameterDescriptor> Parameters { get; private set; }
        public ReadOnlyCollection<APIAttribute> APIAttributes { get; private set; } 
    }
}
