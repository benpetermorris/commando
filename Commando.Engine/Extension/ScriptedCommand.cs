using System;
using System.Collections.Generic;
using System.Linq;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.Load;

namespace twomindseye.Commando.Engine.Extension
{
    public sealed class ScriptedCommand : Command
    {
        readonly string _functionName;

        internal ScriptedCommand(CommandContainer container, LoaderScriptExtension.CommandLoadInfo info)
            : base(container, info.Title, info.Aliases)
        {
            _functionName = info.FunctionName;

            for (var i = 0; i < info.Parameters.Count; i++)
            {
                AddParameterInfo(new ScriptedCommandParameter(this, info.Parameters[i], i));
            }
        }

        internal override void Invoke(IEnumerable<IFacet> facets)
        {
            try
            {
                Container.Invoke(_functionName, facets.ToArray());
            }
            catch (Exception e)
            {
                throw new CommandExecutionException(e.Message, e);
            }
        }
    
        class ScriptedCommandParameter : CommandParameter
        {
            internal ScriptedCommandParameter(ScriptedCommand info, LoaderScriptExtension.ParameterLoadInfo paramInfo, int ordinal)
            {
                Initialize(info, ordinal, paramInfo.Name, paramInfo.Type, paramInfo.Optional, paramInfo.Filters);
            }
        }
    }
}
