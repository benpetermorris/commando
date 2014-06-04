using System;
using System.Windows;
using System.Windows.Controls;

namespace twomindseye.Commando.UI.Util
{
    class DelegatingDataTemplateSelector : DataTemplateSelector
    {
        public Func<object, DependencyObject, DataTemplate> SelectorFunc { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return SelectorFunc(item, container);
        }
    }
}
