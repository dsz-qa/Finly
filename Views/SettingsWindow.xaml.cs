using System.Windows;
using Finly.Services;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly int _userId;

        public SettingsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;

            if (ThemeService.Current == AppTheme.Dark) DarkRadio.IsChecked = true;
            else LightRadio.IsChecked = true;
        }

        private void LightRadio_Checked(object sender, RoutedEventArgs e) => ThemeService.Apply(AppTheme.Light);
        private void DarkRadio_Checked(object sender, RoutedEventArgs e) => ThemeService.Apply(AppTheme.Dark);

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var ask = MessageBox.Show("Na pewno chcesz trwale usunąć konto?",
                                      "Usuń konto", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (ask != MessageBoxResult.Yes) return;

            var ok = UserService.DeleteAccount(_userId);
            if (!ok)
            {
                MessageBox.Show("Nie udało się usunąć konta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var auth = new AuthWindow();
            var vm = (AuthViewModel)auth.DataContext;
            vm.ShowAccountDeletedInfo();

            Application.Current.MainWindow = auth;
            auth.Show();

            if (Owner != null) Owner.Close();
            Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var auth = new AuthWindow();
            var vm = (AuthViewModel)auth.DataContext;
            vm.ShowLogoutInfo();

            Application.Current.MainWindow = auth;
            auth.Show();

            if (Owner != null) Owner.Close();
            Close();
        }
    }
}
