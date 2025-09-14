using System;
using System.Windows.Data;
using System.Globalization;

namespace GamepadEmulator.Views.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public static InverseBoolConverter Instance { get; } = new InverseBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
