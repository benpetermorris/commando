using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace twomindseye.Commando.API1.Extension
{
    public interface IExtensionResolveHandler
    {
        string ResolveExtension(string assemblyName);
    }
}
