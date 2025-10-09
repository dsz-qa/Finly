using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Finly.Helpers
{
    /// <summary>
    /// Konwerter: bool -> Brush
    /// true  => TrueBrush
    /// false => FalseBrush
    /// </summary>
    public sealed class BoolToBrushConverter : IValueConverter
    {
        // Ustawienia domyślne, można nadpisać w XAML
        public Brush TrueBrush { get; set; } = Brushes.ForestGreen;
        public Brush FalseBrush { get; set; } = Brushes.IndianRed;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? TrueBrush : FalseBrush;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Zwykle nie robimy konwersji wstecznej
            return Binding.DoNothing;
        }
    }
}
