using System;
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

            // ====== Ustaw stan kontrolek przy starcie ======
            // Motyw:
            if (ThemeService.Current == AppTheme.Dark)
                DarkRadio.IsChecked = true;
            else
                LightRadio.IsChecked = true;

            // Pozycja tostów:
            ToastPosCombo.Items.Clear();
            ToastPosCombo.Items.Add("Dół – środek");
            ToastPosCombo.Items.Add("Góra – prawa");

            // ustaw wybrane na podstawie ToastService.Position
            ToastPosCombo.SelectedIndex = ToastService.Position switch
            {
                ToastService.ToastPosition.BottomCenter => 0,
                ToastService.ToastPosition.TopRight => 1,
                _ => 0
            };
        }

        // ====== Motywy ======
        private void LightRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ThemeService.Apply(AppTheme.Light);
            ToastService.Success("Włączono motyw jasny.");
        }

        private void DarkRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ThemeService.Apply(AppTheme.Dark);
            ToastService.Success("Włączono motyw ciemny.");
        }

        // ====== Pozycja tostów ======
        private void ToastPosCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            switch (ToastPosCombo.SelectedIndex)
            {
                case 0:
                    ToastService.SetPosition(ToastService.ToastPosition.BottomCenter);
                    ToastService.Info("Komunikaty będą wyświetlane na dole pośrodku.");
                    break;
                case 1:
                    ToastService.SetPosition(ToastService.ToastPosition.TopRight);
                    ToastService.Info("Komunikaty będą wyświetlane w prawym górnym rogu.");
                    break;
            }
        }

        // Opcjonalny przycisk "Przetestuj"
        private void TestToast_Click(object sender, RoutedEventArgs e)
        {
            ToastService.Success("Przykładowy komunikat 😊");
        }
    }
}
