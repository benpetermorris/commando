using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    class DelegatingMultiValueConverter : IMultiValueConverter
    {
        Func<object[], object, object> ConvertFunc { get; set; }

        public void SetConvertFunc<T1, T2>(Func<T1, T2, object, object> func)
        {
            ConvertFunc = (values, parameter) => func((T1) values[0], (T2) values[1], parameter);
        }

        public void SetConvertFunc<T1, T2, T3>(Func<T1, T2, T3, object, object> func)
        {
            ConvertFunc = (values, parameter) => func((T1)values[0], (T2)values[1], (T3)values[2], parameter);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertFunc(values, parameter);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
