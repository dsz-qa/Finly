using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Finly.Helpers
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.ForestGreen;
        public Brush FalseBrush { get; set; } = Brushes.IndianRed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
