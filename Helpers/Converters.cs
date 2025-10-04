namespace Aplikacja_do_sledzenia_wydatkow.Helpers
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.Green;
        public Brush FalseBrush { get; set; } = Brushes.Red;

        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b ? (b ? TrueBrush : FalseBrush) : FalseBrush;

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class BoolToSymbolConverter : IValueConverter
    {
        public string TrueSymbol { get; set; } = "✔";
        public string FalseSymbol { get; set; } = "✖";

        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b ? (b ? TrueSymbol : FalseSymbol) : FalseSymbol;

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }
}
