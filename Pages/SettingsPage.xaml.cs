using System.Windows;
using System.Windows.Controls;
using Finly.Services;

namespace Finly.Pages
{
    public partial class SettingsPage : UserControl
    {
        private bool _isInit;

        // używane przez XAML (bez parametrów)
        public SettingsPage()
        {
            InitializeComponent();

            _isInit = true;

            // Stan motywu
            if (LightRadio != null) LightRadio.IsChecked = ThemeService.Current == AppTheme.Light;
            if (DarkRadio != null) DarkRadio.IsChecked = ThemeService.Current == AppTheme.Dark;

            // Pozycja toastów
            if (ToastPosCombo != null)
            {
                ToastPosCombo.ItemsSource = new[] { "Dół – środek", "Góra – prawa" };
                ToastPosCombo.SelectedIndex =
                    ToastService.Position == ToastService.ToastPosition.TopRight ? 1 : 0;
            }

            _isInit = false;
        }

        // wygodne przeciążenie, gdy chcesz wejść na konkretną zakładkę
        public SettingsPage(string? initialTab) : this()
        {
            // Jeśli masz wewnętrzne zakładki: LoadTab(initialTab ?? "Profile");
        }

        // ===== Handlery =====
        private void LightRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInit) return;
            ThemeService.Apply(AppTheme.Light);
            ToastService.Success("Włączono motyw jasny.");
        }

        private void DarkRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInit) return;
            ThemeService.Apply(AppTheme.Dark);
            ToastService.Success("Włączono motyw ciemny.");
        }

        private void ToastPosCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInit) return;

            ToastService.Position = ToastPosCombo.SelectedIndex == 1
                ? ToastService.ToastPosition.TopRight
                : ToastService.ToastPosition.BottomCenter;

            ToastService.Info("Zmieniono pozycję powiadomień.");
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            ToastService.Success("To jest przykładowe powiadomienie.");
        }
    }
}

