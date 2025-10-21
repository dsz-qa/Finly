using System.Linq;
using System.Windows;

namespace Finly.Services
{
    public enum AppTheme { Light, Dark }

    public static class ThemeService
    {
        public static AppTheme Current { get; private set; } = AppTheme.Dark;

        public static void Apply(AppTheme theme)
        {
            var app = Application.Current;
            if (app is null) return;

            // Usuń dotychczasowe motywy
            var rd = app.Resources.MergedDictionaries;
            var toRemove = rd.Where(d =>
                    d.Source != null &&
                    (d.Source.OriginalString.Contains("Themes/Dark.xaml") ||
                     d.Source.OriginalString.Contains("Themes/Light.xaml")))
                .ToList();
            foreach (var d in toRemove) rd.Remove(d);

            // Dodaj nowy
            var uri = new System.Uri(theme == AppTheme.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml",
                                     System.UriKind.Relative);
            rd.Insert(0, new ResourceDictionary { Source = uri });

            Current = theme;

            // (opcjonalnie) toast informacyjny:
            ToastService.Success(theme == AppTheme.Dark ? "Włączono motyw ciemny." : "Włączono motyw jasny.");
        }
    }
}
