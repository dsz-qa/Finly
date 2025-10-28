// Finly/Helpers/Converters/BoolToBrushConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Finly.Helpers.Converters
{
    /// Konwerter do wiązania: Foreground="{Binding IsOk, Converter={StaticResource BoolToBrush}}"
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.LimeGreen;
        public Brush FalseBrush { get; set; } = Brushes.IndianRed;

        public object Convert(object value, Type t, object parameter, CultureInfo culture)
            => value is bool b && b ? TrueBrush : FalseBrush;

        public object ConvertBack(object value, Type t, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

