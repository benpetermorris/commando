using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.Extension
{
    [DebuggerDisplay("{Name}")]
    public abstract class Command
    {
        readonly CommandContainer _container;
        readonly List<CommandParameter> _commandParameters;
        readonly ReadOnlyCollection<CommandParameter> _commandParametersColl;
        readonly string _name;
        readonly Lazy<string> _signatureHash;
        readonly ReadOnlyCollection<string> _originalAliasesColl; 

        protected Command(CommandContainer container, string name, string[] aliases)
        {
            _container = container;
            _name = name;
            _originalAliasesColl = new ReadOnlyCollection<string>(aliases);
            _commandParameters = new List<CommandParameter>();
            _commandParametersColl = new ReadOnlyCollection<CommandParameter>(_commandParameters);
            _signatureHash = new Lazy<string>(CreateSignatureHash);
        }

        protected void AddParameterInfo(CommandParameter info)
        {
            _commandParameters.Add(info);
        }

        public string Name
        {
            get { return _name; }
        }

        public ReadOnlyCollection<string> Aliases
        {
            get
            {
                return LoaderCommand.Aliases;
            }
        }

        public ReadOnlyCollection<CommandParameter> Parameters
        {
            get { return _commandParametersColl; }
        }

        internal LoaderCommand LoaderCommand { get; set; }

        internal CommandContainer Container
        { 
            get { return _container; } 
        }

        internal string GetSignatureHash()
        {
            return _signatureHash.Value;
        }

        internal ReadOnlyCollection<string> OriginalAliases
        {
            get
            {
                return _originalAliasesColl;
            }
        }

        string CreateSignatureHash()
        {
            var ms = new MemoryStream();

            foreach (var param in _commandParameters)
            {
                var buf = Encoding.Unicode.GetBytes(param.Type.AssemblyQualifiedName);
                ms.Write(buf, 0, buf.Length);
            }

            using (var md5 = new MD5CryptoServiceProvider())
            {
                return md5.ComputeHash(ms.ToArray()).ToHexString();
            }
        }

        internal abstract void Invoke(IEnumerable<IFacet> facets);
    }
}
