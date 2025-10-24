using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Finly.Helpers
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            return b ? Visibility.Collapsed : Visibility.Visible; // odwrotność
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
                return vis != Visibility.Visible; // Visible -> false, Collapsed/Hidden -> true
            return true;
        }
    }
}
