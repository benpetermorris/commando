using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    class EnumerableToStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var e = value as IEnumerable;

            if (e == null)
            {
                return DependencyProperty.UnsetValue;
            }

            var separator = (parameter as string) ?? ", ";

            return string.Join(separator, e.Cast<object>().Select(x => x.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
