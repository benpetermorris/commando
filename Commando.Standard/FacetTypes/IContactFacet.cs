using System;
using System.Collections.Generic;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Standard1.FacetTypes
{
    public enum ContactPhoneType
    {
        Mobile,
        Home,
        Office
    }

    public interface IContactFacet : IFacet
    {
        string Name { get; }
        string Email { get; }
        IEnumerable<Tuple<ContactPhoneType, string>> PhoneNumbers { get; }
    }
}
