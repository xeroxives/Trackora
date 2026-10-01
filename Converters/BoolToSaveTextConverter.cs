using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp14.Converters
{
    public class BoolToSaveTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSaved && isSaved)
                return "❌ Удалить из библиотеки";
            return "💾 Сохранить в библиотеку";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}