using System.Windows;
using System.Windows.Controls;
using Finly.Services;

namespace Finly.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();

            // aktualny motyw
            LightRadio.IsChecked = ThemeService.Current == AppTheme.Light;
            DarkRadio.IsChecked = ThemeService.Current == AppTheme.Dark;

            // pozycje toastów
            ToastPosCombo.ItemsSource = new[] { "Dół – środek", "Góra – prawa" };
            ToastPosCombo.SelectedIndex =
                ToastService.Position == ToastService.ToastPosition.TopRight ? 1 : 0;
        }

        private void LightRadio_Checked(object sender, RoutedEventArgs e)
            => ThemeService.Apply(AppTheme.Light);

        private void DarkRadio_Checked(object sender, RoutedEventArgs e)
            => ThemeService.Apply(AppTheme.Dark);

        private void ToastPosCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ToastService.Position = ToastPosCombo.SelectedIndex == 1
                ? ToastService.ToastPosition.TopRight
                : ToastService.ToastPosition.BottomCenter;

            ToastService.Info("Zmieniono pozycję powiadomień.");
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
            => ToastService.Success("To jest przykładowe powiadomienie.");
    }
}
