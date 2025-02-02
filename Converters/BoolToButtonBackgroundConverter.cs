using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AppLauncher
{
    public class BoolToButtonBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning && isRunning)
            {
                return new SolidColorBrush(Colors.IndianRed);
            }
            return new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}