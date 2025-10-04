using System.Windows;
using System.Windows.Input;
using Finly.Helpers;
using Finly.Services;

namespace Finly.ViewModels
{
    public class RegisterViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public ICommand RegisterCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(Register);
        }

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Login i has³o nie mog¹ byæ puste.", "Rejestracja");
                return;
            }

            // spójnie z UserService: login w wersji kanonicznej
            var normalized = (Username ?? string.Empty).Trim().ToLowerInvariant();

            bool success = UserService.Register(normalized, Password);
            if (success)
            {
                MessageBox.Show("Rejestracja udana!", "Rejestracja");
            }
            else
            {
                MessageBox.Show("Rejestracja nieudana. U¿ytkownik mo¿e ju¿ istnieje.", "Rejestracja");
            }
        }
    }
}
