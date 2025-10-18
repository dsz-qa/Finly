using System.Linq;
using System.Windows;

using Finly.Services;
using Finly.ViewModels;   // AuthViewModel
using Finly.Shell;       // ShellWindow
using Finly.Views;


namespace Finly.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly int _userId;

        public SettingsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;

            // Ustaw stan radiobuttonów zgodnie z aktualnym motywem
            if (ThemeService.Current == AppTheme.Dark)
                DarkRadio.IsChecked = true;
            else
                LightRadio.IsChecked = true;
        }

        private void LightRadio_Checked(object sender, RoutedEventArgs e)
            => ThemeService.Apply(AppTheme.Light);

        private void DarkRadio_Checked(object sender, RoutedEventArgs e)
            => ThemeService.Apply(AppTheme.Dark);

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var ask = MessageBox.Show("Na pewno chcesz trwale usunąć konto?",
                                      "Usuń konto", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (ask != MessageBoxResult.Yes) return;

            var ok = UserService.DeleteAccount(_userId);
            if (!ok)
            {
                MessageBox.Show("Nie udało się usunąć konta.", "Błąd",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var auth = new AuthWindow();
            if (auth.DataContext is AuthViewModel vm)
                vm.ShowAccountDeletedInfo();

            Application.Current.MainWindow = auth;
            auth.Show();

            // ZAMKNIJ SHELLA
            Application.Current.Windows
                .OfType<ShellWindow>()
                .FirstOrDefault()
                ?.Close();

            try { Owner?.Close(); } catch { /* ignore */ }
            Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
                {
                    // 1) Przygotuj ekran logowania z informacją
                    var auth = new AuthWindow();
                    if (auth.DataContext is AuthViewModel vm)
                        vm.ShowLogoutInfo();

                    // 2) Ustaw jako główne okno i pokaż
                    Application.Current.MainWindow = auth;
                    auth.Show();

                    // 3) Zamknij Shella, jeśli jest otwarty
                    Application.Current.Windows
                        .OfType<ShellWindow>()
                        .FirstOrDefault()
                        ?.Close();

                    // 4) Zamknij okno ustawień i ewentualnego właściciela
                    try { Owner?.Close(); } catch { /* ignore */ }
                    Close();
                }

}
}
