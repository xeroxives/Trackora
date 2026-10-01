using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp14.Converters
{
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return new GridLength(0);

            double value = System.Convert.ToDouble(values[0]);
            double maximum = System.Convert.ToDouble(values[1]);

            if (maximum <= 0) return new GridLength(0);

            double ratio = value / maximum;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            return new GridLength(ratio, GridUnitType.Star);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}