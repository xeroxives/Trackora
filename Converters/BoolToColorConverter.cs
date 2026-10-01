using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp14.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
                return new SolidColorBrush(Color.FromRgb(0x1D, 0xB9, 0x54));
            return new SolidColorBrush(Color.FromRgb(0xB3, 0xB3, 0xB3));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}