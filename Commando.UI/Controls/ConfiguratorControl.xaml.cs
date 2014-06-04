using System.Windows;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Engine.Extension;
using twomindseye.Commando.UI.Util;

namespace twomindseye.Commando.UI.Controls
{
    /// <summary>
    /// Interaction logic for ConfiguratorControl.xaml
    /// </summary>
    public partial class ConfiguratorControl
    {
        ConfiguratorBindingProxy _cfgProxy;

        public ConfiguratorControl()
        {
            InitializeComponent();

            Resources.GetResource<DelegatingDataTemplateSelector>("PropertyDataTemplateSelector").SelectorFunc =
                SelectItemTemplate;
        }

        public static readonly DependencyProperty ConfiguratorProperty =
            DependencyProperty.Register("Configurator", typeof (Configurator), typeof (ConfiguratorControl), 
            new PropertyMetadata(default(Configurator), OnConfiguratorChanged));

        public Configurator Configurator
        {
            get
            {
                return (Configurator) GetValue(ConfiguratorProperty);
            }
            set
            {
                SetValue(ConfiguratorProperty, value);
            }
        }

        static void OnConfiguratorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cc = (ConfiguratorControl) d;

            if (cc._cfgProxy != null)
            {
                cc._cfgProxy.Dispose();
                cc._cfgProxy = null;
            }

            if (cc.Configurator == null)
            {
                cc._grid.DataContext = null;
                return;
            }

            var metadata = cc.Configurator.Metadata;
            cc._cfgProxy = new ConfiguratorBindingProxy(cc.Configurator, metadata);
            cc._grid.DataContext = metadata;
        }

        void PropertyItemLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement) sender;

            if (element.DataContext is PropertyItemViewModel)
            {
                // can happen if the same page is re-Loaded 
                return;
            }

            var metadata = (ConfiguratorPropertyMetadata) element.DataContext;
            element.DataContext = new PropertyItemViewModel(_cfgProxy, metadata);
        }

        DataTemplate SelectItemTemplate(object item, DependencyObject container)
        {
            var p = item as PropertyItemViewModel;

            if (p == null)
            {
                return null;
            }

            string resourceKey = null;

            if (p.PropertyMetadata.HasChoices)
            {
                resourceKey = "PropertyTemplateManyChoices";
            }
            else if (p.PropertyMetadata.IsPassword)
            {
                resourceKey = "PropertyTemplatePasswordBox";
            }
            else
            {
                resourceKey = "PropertyTemplateEditBox";
            }

            return (DataTemplate)Resources[resourceKey];
        }

        public sealed class PropertyItemViewModel
        {
            public PropertyItemViewModel(ConfiguratorBindingProxy configuratorProxy, ConfiguratorPropertyMetadata propertyMetadata)
            {
                ConfiguratorProxy = configuratorProxy;
                PropertyMetadata = propertyMetadata;
            }

            public ConfiguratorBindingProxy ConfiguratorProxy { get; private set; }
            public ConfiguratorPropertyMetadata PropertyMetadata { get; private set; }
        }
    }
}
