// Helpers/Converters.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Finly.Helpers
{
    // bool -> Brush (np. SeaGreen / IndianRed)
    [ValueConversion(typeof(bool), typeof(Brush))]
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.SeaGreen;
        public Brush FalseBrush { get; set; } = Brushes.IndianRed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? TrueBrush : FalseBrush;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    // bool -> symbol (np. ✔ / ✖)
    [ValueConversion(typeof(bool), typeof(string))]
    public sealed class BoolToSymbolConverter : IValueConverter
    {
        public string TrueSymbol { get; set; } = "✔";
        public string FalseSymbol { get; set; } = "✖";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? TrueSymbol : FalseSymbol;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    // bool -> Visibility (Visible/Collapsed) z opcjonalnym odwróceniem
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is true;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
