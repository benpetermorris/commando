using System;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine.DB
{
    sealed class FacetUsage
    {
        public FacetUsage(FacetMoniker moniker, Command command, int ordinal, DateTime at, string matchedText)
        {
            if (moniker == null)
            {
                throw new ArgumentNullException("moniker");
            }

            if (matchedText == null)
            {
                throw new ArgumentNullException("matchedText");
            }

            Moniker = moniker;
            Command = command;
            At = at;
            MatchedText = matchedText;
            Ordinal = ordinal;
        }

        public FacetMoniker Moniker { get; private set; }
        public Command Command { get; private set; }
        public DateTime At { get; private set; }
        public string MatchedText { get; private set; }
        public int Ordinal { get; private set; }
    }
}