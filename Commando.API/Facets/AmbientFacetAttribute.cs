using System;

namespace twomindseye.Commando.API1.Facets
{
    [AttributeUsage(AttributeTargets.Class)]
    [Serializable]
    public sealed class AmbientFacetAttribute : APIAttribute
    {
        public AmbientFacetAttribute(string tokenGuid)
        {
            Token = Guid.Parse(tokenGuid);
        }

        public Guid Token { get; private set; }
    }
}
