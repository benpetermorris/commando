using System;
using twomindseye.Commando.API1.Extension;

namespace twomindseye.Commando.API1.Facets
{
    public interface IFacet : IExtensionObject, IDisposable
    {
        string DisplayName { get; }
    }
}