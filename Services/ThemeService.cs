using System.Linq;
using System.Windows;

namespace Finly.Services
{
    public enum AppTheme { Light, Dark }

    public static class ThemeService
    {
        public static AppTheme Current { get; private set; } = AppTheme.Light;

        public static void Apply(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            // usuń stare Theme dictionary
            var old = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/"));
            if (old != null) app.Resources.MergedDictionaries.Remove(old);

            // dołóż nowe
            var uri = new System.Uri(theme == AppTheme.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", System.UriKind.Relative);
            app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = uri });

            Current = theme;
        }
    }
}
