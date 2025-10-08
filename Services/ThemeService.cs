using System;
using System.Windows;

namespace Finly.Services
{
    public enum AppTheme { Light, Dark }

    public static class ThemeService
    {
        private static ResourceDictionary? _current;
        public static AppTheme Current { get; private set; } = AppTheme.Light;

        public static void Apply(AppTheme theme)
        {
            var app = Application.Current;
            if (app is null) return;

            if (_current != null) app.Resources.MergedDictionaries.Remove(_current);

            var uri = new Uri($"Themes/{(theme == AppTheme.Light ? "Light" : "Dark")}.xaml", UriKind.Relative);
            _current = new ResourceDictionary { Source = uri };
            app.Resources.MergedDictionaries.Insert(0, _current); // przed Styles.xaml

            Current = theme;
        }

        public static void Toggle() =>
            Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
    }
}
