using System;
using System.Globalization;
using System.Windows.Data;

namespace Finly.Helpers
{
    [ValueConversion(typeof(bool), typeof(string))]
    public sealed class BoolToSymbolConverter : IValueConverter
    {
        public string TrueSymbol { get; set; } = "✓";
        public string FalseSymbol { get; set; } = "✗";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? TrueSymbol : FalseSymbol;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
