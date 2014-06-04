using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.Load;

namespace twomindseye.Commando.Engine.Extension
{
    [DebuggerDisplay("Name = {Name}, Optional = {Optional}")]
    public abstract class CommandParameter
    {
        FilterExtraDataAttribute[] _filters;

        internal void Initialize(Command info, int ordinal, string name, TypeDescriptor type, bool optional, IEnumerable<FilterExtraDataAttribute> filters = null)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (!type.IsInterface)
            {
                throw new ArgumentException("type must be an interface", "type");
            }

            if (!type.Implements(typeof(IFacet)))
            {
                throw new ArgumentException("type must implement IFacet", "type");
            }

            Command = info;
            Ordinal = ordinal;
            Type = type;
            TypeDescriptor = type;
            Name = name;
            Optional = optional;
            _filters = filters == null ? new FilterExtraDataAttribute[] {} : filters.ToArray();
        }

        public Command Command { get; private set; }
        public int Ordinal { get; private set; }
        public string Name { get; private set; }
        public bool Optional { get; private set; }
        public TypeMoniker Type { get; private set; }
        internal TypeDescriptor TypeDescriptor { get; private set; }

        public bool IsUsableAsArgument(FacetMoniker moniker)
        {
            return TypeDescriptor.Get(moniker.FacetType).Implements(Type) && _filters.All(x => x.Validate(Type, moniker));
        }
    }
}