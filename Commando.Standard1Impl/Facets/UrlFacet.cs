using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.Facets
{
    public sealed class UrlFacet : Facet, IUrlFacet
    {
        readonly string _url;
        readonly string _displayName;

        public UrlFacet(string url)
        {
            _url = url;
            _displayName = url;
        }

        public UrlFacet(string url, string displayName)
        {
            _url = url;
            _displayName = displayName;
        }

        public override string DisplayName
        {
            get { return _displayName; }
        }

        public string Url
        {
            get { return _url; }
        }
    }
}
