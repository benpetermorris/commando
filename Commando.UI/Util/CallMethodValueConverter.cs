using System;
using System.Globalization;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    public sealed class CallMethodValueConverter : IValueConverter
    {
        public string MethodName { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var methodName = parameter as string ?? MethodName;
            
            if (value == null || methodName == null)
            {
                return value;
            }

            var methodInfo = value.GetType().GetMethod(methodName);

            return methodInfo == null ? value : methodInfo.Invoke(value, null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MethodToValueConverter can only be used for one way conversion.");
        }
    }
}
