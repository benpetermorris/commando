using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.UI.ViewModels
{
    class ExtensionsViewModel : ViewModelBase
    {
        readonly Dispatcher _dispatcher;
        readonly WeakEventBridge _bridge;
        readonly ObservableCollection<LoaderExtension> _extensions;

        public ExtensionsViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _extensions = new ObservableCollection<LoaderExtension>();
            Extensions = FilteredReadOnlyObservableCollection.Create(_extensions, 
                ext => ext.Items.OfType<LoaderConfiguratorType>().Any(), 
                ext => ext.Description);
            _bridge = new WeakEventBridge();
            _bridge.Bind(typeof(Loader), "LoadComplete", WeakEventBridge.AsDelegate<EventArgs>(OnLoaderLoadComplete));
            UpdateExtensions();
        }

        void UpdateExtensions()
        {
            foreach (var extension in Loader.Extensions)
            {
                if (!_extensions.Contains(extension))
                {
                    _extensions.Add(extension);
                }
            }
        }

        void OnLoaderLoadComplete(object sender, EventArgs e)
        {
            _dispatcher.Invoke(new Action(UpdateExtensions));
        }

        public ReadOnlyObservableCollection<LoaderExtension> Extensions { get; private set; }
    }
}
