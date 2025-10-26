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

            var rd = app.Resources.MergedDictionaries;

            // 1) Usuń dotychczasowe motywy
            var toRemove = rd.Where(d =>
                d.Source != null &&
                (d.Source.OriginalString.Contains("Themes/Dark.xaml") ||
                 d.Source.OriginalString.Contains("Themes/Light.xaml"))).ToList();
            foreach (var d in toRemove) rd.Remove(d);

            // 2) Znajdź Shared.xaml – chcemy, by motyw był tuż po nim
            var sharedIndex = rd.ToList().FindIndex(d =>
                d.Source != null &&
                d.Source.OriginalString.Replace('\\', '/').EndsWith("Styles/Shared.xaml"));

            var insertIndex = sharedIndex >= 0 ? sharedIndex + 1 : 0;

            // 3) Dodaj nowy motyw
            var uri = new System.Uri(
                theme == AppTheme.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml",
                System.UriKind.Relative);

            rd.Insert(insertIndex, new ResourceDictionary { Source = uri });

            Current = theme;

            // Jeśli masz ToastService – odkomentuj:
            // ToastService.Success(theme == AppTheme.Dark ? "Włączono motyw ciemny." : "Włączono motyw jasny.");
        }
    }
}

