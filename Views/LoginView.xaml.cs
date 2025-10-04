using Aplikacja_do_sledzenia_wydatkow.ViewModels;
using System.Linq;
using System.Windows;

namespace Aplikacja_do_sledzenia_wydatkow.Views
{
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            this.DataContext = _viewModel;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Rêczne przypisanie has³a z PasswordBox
            _viewModel.Username = UsernameBox.Text;
            _viewModel.Password = PasswordBox.Password;

            // Debug: wyœwietlenie loginu i has³a (do sprawdzenia poprawnoœci)
            System.Diagnostics.Debug.WriteLine($"[LOGIN VIEWMODEL] Username: {_viewModel.Username}");
            System.Diagnostics.Debug.WriteLine($"[LOGIN VIEWMODEL] Password: {_viewModel.Password}");

            // Uruchomienie komendy logowania
            if (_viewModel.LoginCommand.CanExecute(null))
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }
    }
}