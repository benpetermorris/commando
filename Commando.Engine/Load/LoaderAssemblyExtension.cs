using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.DB;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine.Load
{
    public sealed class LoaderAssemblyExtension : LoaderExtension
    {
        readonly AppDomain _domain;
        readonly ClientSponsor _clientSponsor;
        readonly AssemblyName _assemblyName;

        internal LoaderAssemblyExtension(string path)
            : base(path)
        {
            var dd = Loader.CreateAssemblyDescriber();
            var describer = dd.Item2;

            try
            {
                _assemblyName = describer.LoadFrom(path);

                TypeDescriptor[] assemblyTypes = null;

                if (GetAssemblyName() != null)
                {
                    foreach (var missing in describer.GetReferencedAssemblies(GetAssemblyName())
                        .Where(x => Loader.FindAssemblyPath(x) == null && !GAC.IsInGAC(x))
                        .ToList())
                    {
                        AddMissingDependency(missing);
                    }

                    if (MissingDependencies.Count > 0)
                    {
                        return;
                    }

                    assemblyTypes = describer.GetTypes(GetAssemblyName());
                }

                if (assemblyTypes == null || assemblyTypes.Length == 0)
                {
                    throw new NotAnExtensionException();
                }

                // TODO: check thumbprint of assembly instead of path
                if (Path.Contains("Test\\Extensions"))
                {
                    var permissions = new PermissionSet(PermissionState.Unrestricted);
                    //permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution | SecurityPermissionFlag.RemotingConfiguration));
                    //permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.NoFlags));
                    //permissions.AddPermission(new FileIOPermission(PermissionState.Unrestricted));
                    //permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery,
                    //  Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
                    //permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery,
                    //    Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)));
                    //permissions.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, "HKEY_LOCAL_MACHINE"));
                    //permissions.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, "HKEY_CURRENT_USER"));
                    //permissions.AddPermission(new RegistryPermission(RegistryPermissionAccess.Write, "HKEY_CURRENT_USER\\Software\\twomindseye\\Commando\\extensions"));

                    var setup = new AppDomainSetup();
                    setup.ApplicationBase = Loader.EngineDirectory;
                    _domain = AppDomain.CreateDomain("Extension: " + _assemblyName.FullName, null, setup, permissions);
                    _clientSponsor = new ClientSponsor();

                    Loader.InitDomainLoader(_domain);
                }

                Description = describer.GetAssemblyTitle(GetAssemblyName()) ?? System.IO.Path.GetFileName(Path);
                Initialize(describer, assemblyTypes);
            }
            finally
            {
                AppDomain.Unload(dd.Item1);
            }
        }

        public AssemblyName GetAssemblyName()
        {
            return new AssemblyName(_assemblyName.ToString());
        }

        protected override void UnloadImpl()
        {
            if (_domain == null)
            {
                throw new InvalidOperationException("not loaded in an AppDomain (internal!)");
            }

            AppDomain.Unload(_domain);
        }

        public override bool OnLoadCompleting(IList<LoaderExtension> extensionSet)
        {
            try
            {
                var allFacetTypeDescriptors = Loader.Extensions
                    .Union(extensionSet)
                    .SelectMany(x => x.Items)
                    .OfType<LoaderFacetType>().Select(x => x.TypeDescriptor)
                    .ToArray();

                foreach (var factory in Items.OfType<LoaderFacetFactory>().ToArray())
                {
                    foreach (var type in factory.Factory.GetFacetTypes())
                    {
                        var typeDesc = TypeDescriptor.TryGet(type);

                        if (!allFacetTypeDescriptors.Contains(typeDesc))
                        {
                            Debug.WriteLine("Factory {0} generates unrecognized facet type {1}", factory.Name, type.FullName);
                            RemoveItem(factory);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        
        void Initialize(AssemblyDescriber describer, TypeDescriptor[] types)
        {
            ExtensionStore.InitExtension(this);

            foreach (var type in types
                .OrderBy(x => x.IsInterface ? 0 : 1)
                .ThenBy(x => x.Implements(typeof(IFacet)) ? 0 : 1))
            {
                try
                {
                    if (type.Implements(typeof(IFacet)))
                    {
                        LoadFacetType(type, describer);
                    }
                    else if (type.Implements(typeof(IFacetFactory)))
                    {
                        LoadFacetFactory(type, describer);
                    }
                    else if (type.Implements(typeof(CommandContainer)))
                    {
                        LoadCommandContainer(type, describer);
                    }
                    else if (type.Implements(typeof(IConfigurator)))
                    {
                        if (!type.IsAbstract && type.HasPublicParameterlessConstructor)
                        {
                            AddItem(new LoaderConfiguratorType(this, type));
                        }
                    }
                }
                catch (LoaderItemInitException e)
                {
                    Debug.WriteLine("Could not load type {0}: {1}", type.FullName, e.Message);
                }
            }
        }

        void LoadFacetFactory(TypeDescriptor factoryType, AssemblyDescriber describer)
        {
            if (factoryType.IsAbstract || !factoryType.HasPublicParameterlessConstructor)
            {
                return;
            }

            var attr = describer.GetClassAttributes(factoryType).OfType<FacetFactoryAttribute>().FirstOrDefault();

            var factory = CreateLoaderInstance<IFacetFactory>(factoryType);
            var info = new LoaderFacetFactory(this, factory, factoryType, attr);
            ExtensionStore.InitFacetFactory(info);
            factory.Initialize(Hooks);
            AddItem(info);
        }

        void LoadCommandContainer(TypeDescriptor containerType, AssemblyDescriber describer)
        {
            if (containerType.IsAbstract || !containerType.HasPublicParameterlessConstructor)
            {
                return;
            }

            var methods = describer.GetPublicMethods(containerType);

            var cmdMethods = (from method in methods
                              let commandAttr = method.APIAttributes.OfType<CommandAttribute>().FirstOrDefault()
                              where commandAttr != null
                              select new { method, commandAttr }).ToArray();

            if (cmdMethods.Length == 0)
            {
                return;
            }

            var dupeCommandNames =
                from i in cmdMethods
                group i by i.commandAttr.Name
                into g
                where g.Count() > 1
                select g;

            if (dupeCommandNames.Any())
            {
                throw new LoaderItemInitException(string.Format("Duplicate command names in container {0}", containerType));
            }

            var container = CreateLoaderInstance<CommandContainer>(containerType);
            var commandInfos = new List<LoaderCommand>();

            foreach (var method in cmdMethods)
            {
                try
                {
                    var reflectedCmd = new ReflectedCommand(container, method.method, method.commandAttr);
                    var lcInfo = new LoaderCommand(this, reflectedCmd);
                    reflectedCmd.LoaderCommand = lcInfo;
                    commandInfos.Add(lcInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not initialize command {0} in container {1}: {2}", method.method.Name, containerType, ex.Message);
                }
            }

            var lccInfo = new LoaderCommandContainer(this, container, containerType);
            ExtensionStore.InitCommandContainer(lccInfo, commandInfos);
            container.Initialize(Hooks);
            AddItems(commandInfos);
            AddItem(lccInfo);
        }

        void LoadFacetType(TypeDescriptor type, AssemblyDescriber describer)
        {
            if (!type.IsInterface && !type.IsAbstract)
            {
                if (!type.GetImplementedTypes(true).Any(x => x.IsInterface && x.Implements(typeof(IFacet))))
                {
                    throw new LoaderItemInitException("None of the interfaces implemented by facet class implements IFacet");
                }
            }

            LoaderAmbientFacet ambientInfo = null;

            if (!type.IsInterface)
            {
                var ambientAttr = describer.GetClassAttributes(type).OfType<AmbientFacetAttribute>().FirstOrDefault();

                if (ambientAttr != null)
                {
                    if (!type.HasPublicParameterlessConstructor || type.IsAbstract)
                    {
                        throw new LoaderItemInitException("No public parameterless constructor for ambient facet type");
                    }

                    var facet = CreateLoaderInstance<IFacet>(type);
                    facet.Initialize(Hooks);
                    ambientInfo = new LoaderAmbientFacet(this, ambientAttr.Token, facet,
                        new FacetMoniker(ambientAttr.Token, type, facet.DisplayName));
                }
            }

            var info = new LoaderFacetType(this, type);
            ExtensionStore.InitFacetType(info);
            AddItem(info);

            if (ambientInfo != null)
            {
                AddItem(ambientInfo);
            }
        }

        public void UnregisterLease(IExtensionObject eo)
        {
            _clientSponsor.Unregister((MarshalByRefObject)(object)eo);
        }

        internal TBase CreateLoaderInstance<TBase>(TypeMoniker type) where TBase : IExtensionObject
        {
            if (_domain == null)
            {
                return (TBase) Activator.CreateInstance(Type.GetType(type.AssemblyQualifiedName));
            }

            var rvl = (TBase)_domain.CreateInstanceAndUnwrap(type.AssemblyName, type.FullName);
            _clientSponsor.Register((MarshalByRefObject)(object)rvl);
            return rvl;
        }
    }
}