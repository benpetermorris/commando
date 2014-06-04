using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Command;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Engine.Extension;
using twomindseye.Commando.Engine.Load;

namespace twomindseye.Commando.UI.Pages
{
    /// <summary>
    /// Interaction logic for ConfiguratorSetWindow.xaml
    /// </summary>
    public partial class ConfiguratorSetPage
    {
        readonly ObservableCollection<ItemViewModel> _viewModels;

        public ConfiguratorSetPage()
        {
            InitializeComponent();

            _viewModels = new ObservableCollection<ItemViewModel>();
            _tabs.ItemsSource = _viewModels;
            SaveCommand = new RelayCommand(SaveAndReturn);
            _saveKeyTrigger.Command = SaveCommand;
            ConfiguratorTypes.CollectionChanged += ConfiguratorsCollChanged;
        }

        static readonly DependencyPropertyKey ConfiguratorTypesPropertyKey =
            DependencyProperty.RegisterReadOnly("ConfiguratorTypes", typeof (ObservableCollection<ConfiguratorSetPageItem>), 
            typeof (ConfiguratorSetPage), new PropertyMetadata(new ObservableCollection<ConfiguratorSetPageItem>()));

        public static readonly DependencyProperty ConfiguratorTypesProperty =
            ConfiguratorTypesPropertyKey.DependencyProperty;

        public ObservableCollection<ConfiguratorSetPageItem> ConfiguratorTypes
        {
            get
            {
                return (ObservableCollection<ConfiguratorSetPageItem>) GetValue(ConfiguratorTypesProperty);
            }
        }

        public ICommand SaveCommand { get; private set; }

        void SaveAndReturn()
        {
            var anySucceded = false;

            foreach (var vm in _viewModels)
            {
                try
                {
                    vm.Configurator.SaveToStore();
                    anySucceded = true;
                }
                catch (Exception)
                {
                }
            }

            OnReturn(new ReturnEventArgs<bool>(anySucceded));
        }

        void ConfiguratorsCollChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (ConfiguratorSetPageItem pageItem in e.NewItems)
            {
                var cfg = pageItem.ConfiguratorType.Create();
                cfg.LoadFromStore();
                var item = new ItemViewModel(cfg, cfg.Metadata, pageItem.ReasonDescription);
                _viewModels.Add(item);
            }
        }

        public sealed class ItemViewModel
        {
            public ItemViewModel(IConfigurator configurator, ConfiguratorMetadata metadata, string reasonDescription)
            {
                Configurator = configurator;
                Metadata = metadata;
                ReasonDescription = reasonDescription;
            }

            public string ReasonDescription { get; private set; }
            public IConfigurator Configurator { get; private set; }
            public ConfiguratorMetadata Metadata { get; private set; }
        }
    }

    public sealed class ConfiguratorSetPageItem
    {
        public ConfiguratorSetPageItem(LoaderConfiguratorType configuratorType, string reasonDescription)
        {
            ConfiguratorType = configuratorType;
            ReasonDescription = reasonDescription;
        }

        public LoaderConfiguratorType ConfiguratorType { get; private set; }
        public string ReasonDescription { get; private set; }
    }
}
