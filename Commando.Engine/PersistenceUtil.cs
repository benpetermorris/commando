using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.DB;

namespace twomindseye.Commando.Engine
{
    public static class PersistenceUtil
    {
        public static void SetFacetMonikerAlias(FacetMoniker moniker, string alias)
        {
            FacetIndex.SaveFacetMoniker(moniker, alias);
        }

        public static void AliasPartialCommand(CommandExecutor userValue, string alias)
        {
            CommandHistory.SavePartialCommand(userValue, alias);
        }
    }
}
