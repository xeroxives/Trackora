using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp14.Converters
{
	[ValueConversion(typeof(double), typeof(string))]
	public class TimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double s && s > 0)
			{
				var ts = TimeSpan.FromSeconds(s);
				return ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"m\:ss");
			}
			return "0:00";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}