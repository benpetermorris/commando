using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace twomindseye.Commando.Engine.Load
{
    [Serializable]
    sealed class ExtensionDependenciesMissingException : Exception
    {
        public ExtensionDependenciesMissingException(string extensionPath, IEnumerable<AssemblyName> missingDependees)
        {
            ExtensionPath = extensionPath;
            MissingDependees = missingDependees.ToArray();
        }

        public string ExtensionPath { get; private set; }
        public AssemblyName[] MissingDependees { get; private set; }
    }
}
