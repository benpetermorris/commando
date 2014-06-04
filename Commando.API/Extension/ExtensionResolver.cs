using System;
using System.Reflection;

namespace twomindseye.Commando.API1.Extension
{
    // TODO: put this in a different common assembly. This is here only so that extension AppDomains don't 
    // have to load Commando.Engine, since the engine uses this type to set up assembly resolution for the
    // extension domain

    public sealed class ExtensionResolver : MarshalByRefObject
    {
        static IExtensionResolveHandler s_handler;
        static bool s_in;

        public ExtensionResolver(IExtensionResolveHandler handler)
        {
            if (s_handler != null)
            {
                throw new InvalidOperationException();
            }

            s_handler = handler;
        }

        static public Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (s_in)
            {
                return null;
            }

            s_in = true;
            var asmpath = s_handler.ResolveExtension(args.Name);
            var asm = asmpath == null ? Assembly.Load(args.Name) : Assembly.LoadFile(asmpath);
            s_in = false;

            return asm;
        }

        static public Assembly HandleReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (s_in)
            {
                return null;
            }

            s_in = true;
            var asmpath = s_handler.ResolveExtension(args.Name);
            var asm = asmpath == null ? Assembly.ReflectionOnlyLoad(args.Name) : Assembly.ReflectionOnlyLoadFrom(asmpath);
            s_in = false;

            return asm;
        }
    }
}
