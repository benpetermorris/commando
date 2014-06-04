using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Engine
{
    public sealed class TextFacet : Facet, ITextFacet
    {
        readonly string _text;

        public TextFacet(string value)
        {
            _text = value;
        }

        public string Text
        {
            get { return _text; }
        }

        public override string DisplayName
        {
            get { return Text; }
        }
    }
}
