using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine.Load
{
    [DebuggerDisplay("{Description} ({Path})")]
    public abstract class LoaderExtension : LoaderItem
    {
        readonly ExtensionHooks _hooks;
        readonly List<LoaderExtensionItem> _items;
        readonly DateTime _fileDateTime;
        readonly byte[] _fileMD5;
        readonly ReadOnlyCollection<AssemblyName> _missingDependenciesColl;
        readonly List<AssemblyName> _missingDependencies; 

        protected LoaderExtension(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            Path = path;
            _fileDateTime = new FileInfo(path).LastWriteTime;
            _fileMD5 = GetFileMD5(Path);
            _hooks = new ExtensionHooks(this);
            _items = new List<LoaderExtensionItem>();
            _missingDependencies = new List<AssemblyName>();
            _missingDependenciesColl = new ReadOnlyCollection<AssemblyName>(_missingDependencies);
        }

        public ICollection<AssemblyName> MissingDependencies
        {
            get
            {
                return _missingDependenciesColl;
            }
        }

        public bool IsReady
        {
            get
            {
                return !IsUnloaded && MissingDependencies.Count == 0;
            }
        }

        protected void AddMissingDependency(AssemblyName dependency)
        {
            _missingDependencies.Add(dependency);
        }

        public string Description { get; protected set; }

        public string Path { get; private set; }

        internal void Unload()
        {
            if (IsUnloaded)
            {
                return;
            }

            UnloadImpl();
            IsUnloaded = true;

            var e = Unloaded;
            if (e != null)
            {
                e(this, null);
            }
        }

        public event EventHandler<EventArgs> Unloaded;

        protected abstract void UnloadImpl();

        public bool IsUnloaded { get; private set; }

        public IEnumerable<LoaderExtensionItem> Items
        {
            get
            {
                return _items.AsEnumerable();
            }
        }

        public LoaderConfiguratorType GetConfiguratorFor(TypeMoniker extensionItemType)
        {
            return _items
                .OfType<LoaderConfiguratorType>()
                .Where(x => x.Metadata.ConfiguresType(extensionItemType))
                .FirstOrDefault();
        }

        public bool AreConfiguratorsConfigured()
        {
            return _items
                .OfType<LoaderConfiguratorType>()
                .Select(x => x.Create())
                .All(x => x.IsConfigured());
        }

        protected void AddItem(LoaderExtensionItem item)
        {
            lock (_items)
            {
                _items.Add(item);
            }
        }

        protected void AddItems(IEnumerable<LoaderExtensionItem> items)
        {
            lock (_items)
            {
                _items.AddRange(items);
            }
        }

        protected void RemoveItem(LoaderExtensionItem item)
        {
            lock (_items)
            {
                _items.Remove(item);
            }
        }

        internal IExtensionHooks Hooks
        {
            get
            {
                return _hooks;
            }
        }

        class ExtensionHooks : MarshalByRefObject, IExtensionHooks
        {
            readonly LoaderExtension _extension;
            readonly Lazy<KeyValueStore> _store;

            public ExtensionHooks(LoaderExtension extension)
            {
                _extension = extension;
                _store = new Lazy<KeyValueStore>(CreateKeyValueStore);
            }

            KeyValueStore CreateKeyValueStore()
            {
                if (_extension.DatabaseId == 0)
                {
                    throw new InvalidOperationException("DatabaseId not set");
                }

                return new KeyValueStore(_extension.DatabaseId);
            }

            public IKeyValueStore GetKeyValueStore()
            {
                return _store.Value;
            }
        }


        static byte[] GetFileMD5(string path)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return md5.ComputeHash(File.ReadAllBytes(path));
            }
        }

        internal bool IsUpdated(string path)
        {
            return new FileInfo(path).LastWriteTime != _fileDateTime && 
                !GetFileMD5(path).SequenceEqual(_fileMD5);
        }

        internal void OnLoadComplete()
        {
            OnLoadCompleteImpl();

            foreach (var item in _items)
            {
                item.OnLoadComplete();
            }
        }

        protected virtual void OnLoadCompleteImpl()
        {
        }

        public abstract bool OnLoadCompleting(IList<LoaderExtension> extensionSet);
    }
}