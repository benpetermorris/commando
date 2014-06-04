using System;

namespace twomindseye.Commando.API1.Facets
{
    [AttributeUsage(AttributeTargets.Class)]
    [Serializable]
    public sealed class FacetFactoryAttribute : APIAttribute
    {
        public FacetFactoryAttribute(string name)
        {
            Name = name;
            Aliases = string.Empty;
        }

        public string Name { get; private set; }
        public string Aliases { get; set; }
        public string[] AliasesSplit
        {
            get { return Aliases.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
        }
    }
}
