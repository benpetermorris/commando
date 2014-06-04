using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace twomindseye.Commando.UI.Util
{
    [ContentProperty("Template")]
    public sealed class BooleanDataTemplateSelectorItem : FrameworkElement
    {
        public DataTemplate Template { get; set; }

        public static readonly DependencyProperty IsChosenProperty =
            DependencyProperty.Register("IsChosen", typeof (bool), typeof (BooleanDataTemplateSelectorItem), 
            new PropertyMetadata(default(bool)));

        public bool IsChosen
        {
            get
            {
                return (bool) GetValue(IsChosenProperty);
            }
            set
            {
                SetValue(IsChosenProperty, value);
            }
        }
    }

    [ContentProperty("Items")]
    public sealed class BooleanDataTemplateSelector : DataTemplateSelector
    {
        static readonly DataTemplate s_emptyDataTemplate = new DataTemplate();

        public BooleanDataTemplateSelector()
        {
            Items = new Collection<BooleanDataTemplateSelectorItem>();
        }

        public Collection<BooleanDataTemplateSelectorItem> Items { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return Items.Where(x => x.IsChosen).Select(x => x.Template).FirstOrDefault() ?? s_emptyDataTemplate;
        }
    }
}
