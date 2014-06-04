using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.UI.ViewModels;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.UI.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
        public SettingsPage()
        {
            DataContext = new ExtensionsViewModel(Dispatcher);

            InitializeComponent();

            ConfigureExtensionCommand = new RelayCommand<LoaderExtension>(ConfigureExtension, ex => ex != null);
        }

        void ConfigureExtension(LoaderExtension extension)
        {
            var configuratorSetPage = new ConfiguratorSetPage();

            foreach (var configurator in extension.Items.OfType<LoaderConfiguratorType>())
            {
                configuratorSetPage.ConfiguratorTypes.Add(new ConfiguratorSetPageItem(configurator, null));
            }

            NavigationService.Navigate(configuratorSetPage);

            configuratorSetPage.Return += ConfiguratorSetPageReturn;
        }

        void ConfiguratorSetPageReturn(object sender, System.Windows.Navigation.ReturnEventArgs<bool> e)
        {
            _list.Items.Refresh();
        }

        public RelayCommand<LoaderExtension> ConfigureExtensionCommand { get; private set; }
    }
}
