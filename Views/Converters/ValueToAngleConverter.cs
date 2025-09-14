using System;
using System.Windows.Data;

namespace GamepadEmulator.Views.Converters
{
    public class ValueToAngleConverter : IValueConverter
    {
        public static ValueToAngleConverter Instance { get; } = new ValueToAngleConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Convert slider value (0-100) to angle (-135 to +135 degrees)
                return (doubleValue / 100.0) * 270 - 135;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
