using System;
using System.Collections.Generic;
using System.Linq;
using twomindseye.Commando.API1.Extension;

namespace twomindseye.Commando.API1.Commands
{
    public abstract class CommandContainer : ExtensionObject
    {
        public void Invoke(string methodName, object[] parameters)
        {
            var methodInfo = GetType().GetMethod(methodName);

            try
            {
                methodInfo.Invoke(this, parameters);
            }
            catch (AssemblyDecoupledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // decoupled to ensure exception types thrown by an extension don't need to be loaded
                // into the engine
                throw new AssemblyDecoupledException(ex);
            }
        }
    }
}
