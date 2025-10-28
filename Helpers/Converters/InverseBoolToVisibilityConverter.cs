// Finly/Helpers/Converters/InverseBoolToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Finly.Helpers.Converters
{
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type t, object parameter, CultureInfo culture)
            => !(value is Visibility v && v == Visibility.Visible);
    }
}
