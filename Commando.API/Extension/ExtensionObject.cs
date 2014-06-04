using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace twomindseye.Commando.API1.Extension
{
    public abstract class ExtensionObject : MarshalByRefObject, IExtensionObject
    {
        bool _initialized;

        protected IExtensionHooks ExtensionHooks { get; private set; }

        public virtual void Initialize(IExtensionHooks extensionHooks)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("Already initialized");
            }

            _initialized = true;
            ExtensionHooks = extensionHooks;
        }
    }
}
