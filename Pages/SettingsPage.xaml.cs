using System.Windows;
using System.Windows.Controls;
using Finly.Services;

namespace Finly.Pages
{
    public partial class SettingsPage : UserControl
    {
        private bool _isInit;

        public SettingsPage()
        {
            InitializeComponent();

            _isInit = true;

            // Stan bieżącego motywu (bez odpalania zdarzeń)
            LightRadio.IsChecked = ThemeService.Current == AppTheme.Light;
            DarkRadio.IsChecked = ThemeService.Current == AppTheme.Dark;

            // Pozycje tostów (bez wywoływania SelectionChanged)
            ToastPosCombo.ItemsSource = new[] { "Dół – środek", "Góra – prawa" };
            ToastPosCombo.SelectedIndex =
                ToastService.Position == ToastService.ToastPosition.TopRight ? 1 : 0;

            _isInit = false;
        }

        // Motyw
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

        // Pozycja toastów
        private void ToastPosCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInit) return;

            ToastService.Position = ToastPosCombo.SelectedIndex == 1
                ? ToastService.ToastPosition.TopRight
                : ToastService.ToastPosition.BottomCenter;

            ToastService.Info("Zmieniono pozycję powiadomień.");
        }

        // Testowy toast („Przetestuj” i „Wyświetl test”)
        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            ToastService.Success("To jest przykładowe powiadomienie.");
        }
    }
}
