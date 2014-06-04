using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace twomindseye.Commando.UI.Util
{
    [ContentProperty("Converters")]
    class CascadingValueConverter : DependencyObject, IValueConverter
    {
        public CascadingValueConverter()
        {
            Converters = new List<IValueConverter>();
        }

        public List<IValueConverter> Converters { get; private set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Converters.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
