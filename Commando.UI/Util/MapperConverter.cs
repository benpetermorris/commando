using System;
using System.Windows;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    [ContentProperty("Map")]
    public class MapperConverter : IValueConverter
    {
        public MapperConverter()
        {
            Map = new Collection<ValueMapping>();
        }

        public Collection<ValueMapping> Map { get; private set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var valString = value == null ? "*null" : value.ToString();
            var defaultValue = value;

            foreach (var v in Map)
            {
                var vvalue = v.Value;

                if (v.Key == "*nonnull" && value != null)
                {
                    return vvalue;
                }
                
                if (v.Key == "*null" && value == null)
                {
                    return vvalue;
                }
                
                if (v.Key == "*default")
                {
                    defaultValue = vvalue;
                    continue;
                }

                if (v.Key.StartsWith("*type:") && value != null && string.Compare(value.GetType().Name, v.Key.Substring(6), true) == 0)
                {
                    return vvalue;
                }

                if (string.Compare(v.Key, valString, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    return vvalue;
                }
            }

            return defaultValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
