using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp14.Converters
{
    public class PercentWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return 0.0;

            double value = System.Convert.ToDouble(values[0]);      // Current Position
            double actualWidth = System.Convert.ToDouble(values[1]); // Total Width of Bar
            double maximum = System.Convert.ToDouble(values[2]);     // Total Duration

            if (maximum <= 0 || actualWidth <= 0) return 0.0;

            // Формула: (Текущее / Максимум) * ШиринаКонтейнера
            double percentage = value / maximum;

            // Ограничиваем от 0 до 1
            if (percentage < 0) percentage = 0;
            if (percentage > 1) percentage = 1;

            return percentage * actualWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}