using System.Windows;
using System.Windows.Media;

namespace twomindseye.Commando.UI.Controls
{
    public class ConfiguratorPropertyDataTemplateSelector : System.Windows.Controls.DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var p = item as ConfiguratorControl.PropertyItemViewModel;

            if (p == null)
            {
                return null;
            }

            ConfiguratorControl cc = null;

            while (cc == null && container != null)
            {
                container = VisualTreeHelper.GetParent(container);
                cc = container as ConfiguratorControl;
            }

            if (cc == null)
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

            return (DataTemplate)cc.Resources[resourceKey];
        }
    }
}