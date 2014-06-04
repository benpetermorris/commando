using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.Extension;
using twomindseye.Commando.Util;

// TODO: watch for directory changes
// TODO: consider loading assemblies into s_loaderDomain instead of main domain

namespace twomindseye.Commando.Engine.Load
{
    public static class Loader
    {
        static readonly ConcurrentBag<string> s_directories;
        static readonly ConcurrentBag<LoaderExtension> s_extensions;
        static readonly object s_refreshLock;
        static readonly ExtensionResolveHandler s_resolveHandler;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetDllDirectory(string lpPathName);

        static Loader()
        {
            // TODO: fix
            //SetDllDirectory(@"c:\projects\twomindseye\commando\test\extensions");

            s_directories = new ConcurrentBag<string>();
            s_extensions = new ConcurrentBag<LoaderExtension>();
            s_refreshLock = new object();
            s_resolveHandler = new ExtensionResolveHandler();

            s_extensions.Add(Load(Assembly.GetExecutingAssembly().Location));
            s_extensions.Add(Load(typeof(IFacetFactory).Assembly.Location));
        }

        public static void AddExtensionDirectories(IEnumerable<string> directories)
        {
            // only lock is here, simply to avoid duplicates
            lock (s_directories)
            {
                if (s_directories.Any(x => !Directory.Exists(x)))
                {
                    throw new DirectoryNotFoundException();
                }

                foreach (var directory in directories)
                {
                    if (s_directories.Contains(directory))
                    {
                        return;
                    }

                    s_directories.Add(directory);
                }
            }

            Refresh();
        }

        public static string[] GetExtensionDirectories()
        {
            return s_directories.ToArray();
        }

        static void Refresh()
        {
            var loaded = new ConcurrentBag<LoaderExtension>();

            lock (s_refreshLock)
            {
                var items = (from directory in s_directories
                             from path in Directory.EnumerateFiles(directory)
                             where IsExtensionPath(path)
                             let existing = Extensions.FirstOrDefault(x => x.Path == path)
                             where existing == null || !existing.IsReady || existing.IsUpdated(path)
                             let isScript = Path.GetExtension(path).ToLower() == ".js"
                             orderby isScript
                             select new
                                    {
                                        Existing = existing,
                                        Path = path,
                                        IsScript = isScript
                                    }).ToArray();

#if NO
                var dd = CreateAssemblyDescriber();

                try
                {
                    var names = new Dictionary<string, AssemblyName>();
                    var dependencies = new Dictionary<string, AssemblyName[]>();

                    foreach (var item in items.Where(x => !x.IsScript))
                    {
                        var name = dd.Item2.LoadFrom(item.Path);
                        names[item.Path] = name;
                        dependencies[item.Path] = dd.Item2.GetReferencedAssemblies(name).Where(x => !GAC.IsInGAC(x)).ToArray();
                    }

                    foreach (var item in items.Where(x => !x.IsScript))
                    {
                        var itemDependencies = dependencies[item.Path];

                        if (itemDependencies.Any(x => FindAssemblyPath(x) == null))
                        {
                            // TODO: can't load this one
                            continue;
                        }

                        // trim down dependencies to those we're about to load
                        itemDependencies = dependencies[item.Path] = dependencies[item.Path]
                            .Intersect(names.Values, TypeDescriptor.AssemblyNameComparer)
                            .ToArray();
                    }

                    var loadOrder = new List<string>();

                    loadOrder.AddRange(dependencies.Where(x => x.Value.Length == 0).Select(x => x.Key));

                    foreach (var item in dependencies.Where(x => x.Value.Length > 0).OrderBy(x => x.Value.Length))
                    {

                    }
                }
                finally
                {
                    AppDomain.Unload(dd.Item1);
                }
#endif
                Parallel.ForEach(items,
                    item =>
                    {
                        if (item.Existing != null && item.Existing.IsReady)
                        {
                            item.Existing.Unload();
                        }

                        var extension = Load(item.Path, loaded.ToList());

                        if (extension != null)
                        {
                            lock (loaded)
                            {
                                if (!IsExtensionLoaded(extension, loaded.ToList()))
                                {
                                    loaded.Add(extension);
                                }
                            }
                        }
                    });

                var loadedList = loaded.ToList();

                Parallel.ForEach(loaded,
                    extension =>
                    {
                        // perf check
                        if (!IsExtensionLoaded(extension, Extensions) && 
                            extension.OnLoadCompleting(loadedList))
                        {
                            lock (s_extensions)
                            {
                                // definitive check
                                if (!IsExtensionLoaded(extension, Extensions))
                                {
                                    s_extensions.Add(extension);
                                }
                            }
                        }
                    });

                foreach (var extension in loaded)
                {
                    extension.OnLoadComplete();
                }
            }

            if (!loaded.IsEmpty)
            {
                FireLoadComplete();
            }
        }

        internal static bool IsExtensionPath(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext == ".js" || ext == ".dll";
        }

        static LoaderExtension Load(string extensionPath, List<LoaderExtension> pending = null)
        {
            var ext = Path.GetExtension(extensionPath).ToLower();
            var extList = Extensions.ToList();

            if (pending != null)
            {
                extList.AddRange(pending);
            }

            Debug.WriteLine("Loading extension {0}", (object)extensionPath);

            try
            {
                switch (ext)
                {
                    case ".dll":
                        var asmName = AssemblyName.GetAssemblyName(extensionPath);
                        if (!IsExtensionLoaded(asmName, extList))
                        {
                            return new LoaderAssemblyExtension(extensionPath);
                        }
                        Debug.WriteLine("WARNING: extension with identical AssemblyName ignored ({0})", asmName);
                        break;
                    case ".js":
                        return new LoaderScriptExtension(extensionPath);
                    default:
                        throw new ArgumentException("extensionPath");
                }
            }
            catch (NotAnExtensionException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not load extension {0}: {1}", extensionPath, ex.Message);
            }

            return null;
        }

        static bool IsExtensionLoaded(AssemblyName asmName, IList<LoaderExtension> extList)
        {
            return extList
                    .OfType<LoaderAssemblyExtension>()
                    .Any(x => x.GetAssemblyName().FullName == asmName.FullName);
        }

        private static bool IsExtensionLoaded(LoaderExtension ext, IList<LoaderExtension> extList)
        {
            var lae = ext as LoaderAssemblyExtension;

            if (lae != null)
            {
                return IsExtensionLoaded(lae.GetAssemblyName(), extList);
            }

            var lse = ext as LoaderScriptExtension;

            if (lse != null)
            {
                return extList
                    .OfType<LoaderScriptExtension>()
                    .Any(x => x.Path == lse.Path);
            }

            return false;
        }

        public static LoaderExtension[] Extensions
        {
            get
            {
                lock (s_extensions)
                {
                    return s_extensions.Where(x => !x.IsUnloaded).ToArray();
                }
            }
        }

        public static LoaderExtensionItem[] ExtensionItems
        {
            get
            {
                return Extensions.SelectMany(x => x.Items).ToArray();
            }
        }

        public static IEnumerable<LoaderFacetFactory> FacetFactories
        {
            get
            {
                return ExtensionItems.OfType<LoaderFacetFactory>();
            }
        }

        public static IEnumerable<LoaderCommand> Commands
        {
            get
            {
                return ExtensionItems.OfType<LoaderCommand>();
            }
        }

        public static IEnumerable<LoaderAmbientFacet> AmbientFacets
        {
            get
            {
                return ExtensionItems.OfType<LoaderAmbientFacet>();
            }
        }

        public static IEnumerable<LoaderFacetType> FacetTypes
        {
            get
            {
                return ExtensionItems.OfType<LoaderFacetType>();
            }
        }

        public static IEnumerable<LoaderConfiguratorType> ConfiguratorTypes
        {
            get
            {
                return ExtensionItems.OfType<LoaderConfiguratorType>();
            }
        }

        public static string EngineDirectory
        {
            get
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(asmPath);
            }
        }

        internal static LoaderAmbientFacet GetAmbientFacet(Guid ambientToken)
        {
            return AmbientFacets.Where(x => x.AmbientToken == ambientToken).SingleOrDefault();
        }

        internal static IEnumerable<LoaderFacetType> GetFacetTypesForParameterTypes(IEnumerable<TypeDescriptor> parameterTypes)
        {
            return FacetTypes.Where(ft => parameterTypes.Any(pt => ft.TypeDescriptor.Implements(pt)));
        }

        internal static LoaderFacetFactory GetFactoryInfo(IFacetFactory factory)
        {
            return FacetFactories.Where(x => x.Factory == factory).Single();
        }

        internal static LoaderFacetFactory GetFactoryInfo(TypeMoniker factoryType)
        {
            return FacetFactories.Where(x => x.Type == factoryType).Single();
        }

        internal static LoaderFacetFactory GetFactoryInfo(int databaseId)
        {
            return FacetFactories.Where(x => x.DatabaseId == databaseId).SingleOrDefault();
        }

        internal static LoaderCommand GetCommandInfo(Command command)
        {
            return Commands.Where(x => x.Command == command).Single();
        }

        internal static LoaderCommand GetCommandInfo(int databaseId)
        {
            return Commands.Where(x => x.DatabaseId == databaseId).SingleOrDefault();
        }

        internal static LoaderFacetType GetFacetInfo(TypeMoniker facetType)
        {
            return FacetTypes.Where(x => x.Type == facetType).Single();
        }

        internal static LoaderFacetType GetFacetInfo(int databaseId)
        {
            return FacetTypes.Where(x => x.DatabaseId == databaseId).SingleOrDefault();
        }

        /// <summary>
        /// Returns the set of factories that can each generate at least one of the facet types specified.
        /// </summary>
        internal static IEnumerable<LoaderFacetFactory> GetFacetFactoriesForFacetTypes(IEnumerable<TypeDescriptor> facetTypes)
        {
            var parameterTypesArray = facetTypes.ToArray();

            return from fac in FacetFactories
                   where fac.FacetTypeDescriptors.Intersect(parameterTypesArray).Any()
                   select fac;
        }

        internal static IEnumerable<LoaderFacetType> GetFacetTypesImplementing(TypeMoniker type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return FacetTypes.Where(x => x.TypeDescriptor.Implements(type));
        }

        static void FireLoadComplete()
        {
            var e = LoadComplete;

            if (e != null)
            {
                e(null, null);
            }
        }

        public static event EventHandler<EventArgs> LoadComplete;

        internal static string FindAssemblyPath(AssemblyName name)
        {
            var query = from d in s_directories.Concat(EngineDirectory).Distinct()
                        let path = Path.Combine(d, name.Name) + ".dll"
                        where File.Exists(path)
                        select path;

            return query.FirstOrDefault();
        }

        //public static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    var name = new AssemblyName(args.Name);
        //    var asmpath = FindAssemblyPath(name);
        //    return asmpath == null ? Assembly.Load(name) : Assembly.LoadFile(asmpath);
        //}

        //public static Assembly HandleReflectionAssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    var name = new AssemblyName(args.Name);
        //    var asmpath = FindAssemblyPath(name);
        //    return asmpath == null ? Assembly.ReflectionOnlyLoad(name.ToString()) : Assembly.ReflectionOnlyLoadFrom(asmpath);
        //}

        internal static void InitDomainLoader(AppDomain domain)
        {
            var resolverType = typeof(ExtensionResolver);
            var loaderResolver = (ExtensionResolver)domain.CreateInstanceAndUnwrap(
                resolverType.Assembly.FullName, resolverType.FullName, false, 0, null, new[] { (IExtensionResolveHandler)s_resolveHandler }, null, null);
            domain.AssemblyResolve += ExtensionResolver.HandleAssemblyResolve;
            domain.ReflectionOnlyAssemblyResolve += ExtensionResolver.HandleReflectionOnlyAssemblyResolve;
        }

        internal static Tuple<AppDomain, AssemblyDescriber> CreateAssemblyDescriber()
        {
            var setup = new AppDomainSetup();
            setup.ApplicationBase = EngineDirectory;

            var describerDomain = AppDomain.CreateDomain("Describer Domain", null, setup);
            InitDomainLoader(describerDomain);

            var describer = (AssemblyDescriber)describerDomain.CreateInstanceAndUnwrap(
                Assembly.GetExecutingAssembly().FullName, typeof(AssemblyDescriber).FullName);

            return Tuple.Create(describerDomain, describer);
        }

        sealed class ExtensionResolveHandler : MarshalByRefObject, IExtensionResolveHandler
        {
            public string ResolveExtension(string assemblyName)
            {
                return FindAssemblyPath(new AssemblyName(assemblyName));
            }
        }
    }
}