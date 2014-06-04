using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.DB;

namespace twomindseye.Commando.Engine.Load
{
    [DebuggerDisplay("{Name}")]
    public sealed class LoaderFacetFactory : LoaderExtensionItem
    {
        ReadOnlyCollection<string> _aliases;
        List<RequiresConfigurationException> _indexingConfigurationExceptions;
        bool _isIndexed;
        readonly Lazy<ReadOnlyCollection<TypeDescriptor>> _facetTypeDescriptors;

        internal LoaderFacetFactory(LoaderExtension extension, IFacetFactory factory, TypeDescriptor type, 
            FacetFactoryAttribute attribute) 
            : base(extension)
        {
            _facetTypeDescriptors = new Lazy<ReadOnlyCollection<TypeDescriptor>>(
                () => new ReadOnlyCollection<TypeDescriptor>(Factory.GetFacetTypes().Select(TypeDescriptor.Get).ToArray()));

            Factory = factory;
            TypeDescriptor = type;
            Type = type;
            Name = attribute == null ? type.FullName : attribute.Name;
            _aliases = new ReadOnlyCollection<string>(attribute == null ? new string[] { } : attribute.AliasesSplit);
            _isIndexed = Factory is IFacetFactoryWithIndex;
            if (_isIndexed)
            {
                _indexingConfigurationExceptions = new List<RequiresConfigurationException>();
            }
        }

        public string Name { get; private set; }

        public TypeMoniker Type { get; set; }

        public override LoaderConfiguratorType ConfiguratorType
        {
            get
            {
                return Extension.GetConfiguratorFor(Type);
            }
        }

        public ReadOnlyCollection<string> Aliases
        {
            get
            {
                return _aliases;
            }
        }

        public bool IsIndexed
        {
            get
            {
                return _isIndexed;
            }
        }

        public bool IndexRequiresConfiguration
        {
            get
            {
                return _indexingConfigurationExceptions.Any();
            }
        }

        internal void AddIndexingException(RequiresConfigurationException rce)
        {
            lock (_indexingConfigurationExceptions)
            {
                _indexingConfigurationExceptions.Add(rce);
            }
        }

        internal override void OnLoadComplete()
        {
            base.OnLoadComplete();

            if (IsIndexed)
            {
                _indexingConfigurationExceptions.Clear();
                FacetIndex.TryUpdateFacetFactoryIndex(this);
            }
        }

        internal override string DatabaseName
        {
            get
            {
                return Type.FullName;
            }
        }

        internal ReadOnlyCollection<TypeDescriptor> FacetTypeDescriptors
        {
            get
            {
                return _facetTypeDescriptors.Value;
            }
        }

        internal void SetAliases(string[] aliases)
        {
            _aliases = new ReadOnlyCollection<string>(aliases.ToArray());
            RaisePropertyChanged("Aliases");
        }

        internal TypeDescriptor TypeDescriptor { get; private set; }

        internal IFacetFactory Factory { get; private set; }

        internal IFacet CreateFacet(FacetMoniker moniker)
        {
            var facet = Factory.CreateFacet(moniker);

            if (facet != null)
            {
                facet.Initialize(Extension.Hooks);
            }

            return facet;
        }
    }
}