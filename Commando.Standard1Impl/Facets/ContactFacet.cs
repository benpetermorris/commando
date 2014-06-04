using System;
using System.Collections.Generic;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.Facets
{
    public sealed class ContactFacet : Facet, IContactFacet, IEmailAddressFacet
    {
        readonly string _name;
        readonly string _email;

        public ContactFacet(string name, string email)
        {
            _name = name;
            _email = email;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Email
        {
            get { return _email; }
        }

        public IEnumerable<Tuple<ContactPhoneType, string>> PhoneNumbers
        {
            get { return null; }
        }

        string IEmailAddressFacet.Value
        {
            get { return Email; }
        }

        public override string DisplayName { get { return _name; } }
    }
}
