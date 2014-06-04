using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    sealed class ComparisonValueConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty NegativeResultProperty =
            DependencyProperty.Register("NegativeResult", typeof (object), typeof (ComparisonValueConverter), 
            new PropertyMetadata(default(object)));

        public object NegativeResult
        {
            get
            {
                return (object) GetValue(NegativeResultProperty);
            }
            set
            {
                SetValue(NegativeResultProperty, value);
            }
        }

        public static readonly DependencyProperty PositiveResultProperty =
            DependencyProperty.Register("PositiveResult", typeof (object), typeof (ComparisonValueConverter), 
            new PropertyMetadata(default(object)));

        public object PositiveResult
        {
            get
            {
                return (object) GetValue(PositiveResultProperty);
            }
            set
            {
                SetValue(PositiveResultProperty, value);
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return NegativeResult;
            }

            return value.ToString() == parameter.ToString() ? PositiveResult : NegativeResult;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
