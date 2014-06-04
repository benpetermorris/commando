using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    public sealed class BooleanMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var mode = parameter as string ?? "alltrue";
            var boolValues = values.OfType<bool>();

            switch (mode)
            {
                case "alltrue":
                    return boolValues.All(x => x);
                case "allfalse":
                    return boolValues.All(x => !x);
                case "anytrue":
                    return boolValues.Any(x => x);
                case "anyfalse":
                    return boolValues.Any(x => !x);
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
