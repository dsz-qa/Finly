using Aplikacja_do_œledzenia_wydatków.Services;
using Aplikacja_do_sledzenia_wydatkow.Views;
using System.Windows.Input;
using System.Windows;
using Aplikacja_do_sledzenia_wydatkow.Helpers;

namespace Aplikacja_do_sledzenia_wydatkow.ViewModels
{
    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public ICommand LoginCommand { get; set; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
        }

        private void Login()
        {
            System.Diagnostics.Debug.WriteLine($"[LOGIN VIEWMODEL] Username: {Username}");
            System.Diagnostics.Debug.WriteLine($"[LOGIN VIEWMODEL] Password: {Password}");
            System.Diagnostics.Debug.WriteLine($"[LOGIN VIEWMODEL] Przekazywane has³o: {Password}");

            if (UserService.Login(Username, Password))
            {
                // Pobierz userId z bazy danych
                int userId = UserService.GetUserIdByUsername(Username);

                // Otwórz Dashboard
                var dashboard = new DashboardView(userId);
                dashboard.Show();

                // Zamknij LoginView
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.LoginView)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("B³êdny login lub has³o.");
            }
        }
    }
}