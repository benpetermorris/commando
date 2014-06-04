using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.Facets
{
    public sealed class EmailAddressFacet : Facet, IEmailAddressFacet
    {
        readonly string _value;

        public EmailAddressFacet(string value)
        {
            _value = value;
        }

        public override string DisplayName
        {
            get { return Value; }
        }

        public string Value
        {
            get { return _value; }
        }
    }
}
