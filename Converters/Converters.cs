using System.Globalization;
using System.Windows.Data;

namespace AppLauncher
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolean ? !boolean : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolean ? !boolean : value;
        }
    }
}