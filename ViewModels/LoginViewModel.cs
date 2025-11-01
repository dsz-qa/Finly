using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Finly.Helpers;   // RelayCommand
using Finly.Services;  // UserService
using Finly.Views;     // DashboardView, LoginView
using Finly.Shell;          // ShellWindow


namespace Finly.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _password = string.Empty;

        public string Username
        {
            get => _username;
            set
            {
                if (_username == (value ?? string.Empty)) return;
                _username = value ?? string.Empty;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password == (value ?? string.Empty)) return;
                _password = value ?? string.Empty;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            // CanExecute blokuje przycisk gdy pola s� puste
            LoginCommand = new RelayCommand(Login, CanLogin);
        }

        private bool CanLogin()
            => !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password);

        private void Login()
        {
            try
            {
                var user = (Username ?? string.Empty).Trim().ToLowerInvariant();
                var pass = Password ?? string.Empty;

                if (!CanLogin())
                {
                    MessageBox.Show("Podaj login i has�o.", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UserService.Login(user, pass))
                {
                    MessageBox.Show("B��dny login lub has�o.", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //  Sukces: pobierz Id i otw�rz Dashboard
                int userId = UserService.GetUserIdByUsername(user);
                if (userId <= 0)
                {
                    MessageBox.Show("Nie uda�o si� pobra� identyfikatora u�ytkownika.", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Zapisz ID aktualnie zalogowanego u�ytkownika
                UserService.CurrentUserId = userId;

                //  Utw�rz schemat bazy (bez seedowania kategorii)
                DatabaseService.EnsureCoreTables();

                //  USUWAMY SEEDOWANIE � �adnych domy�lnych kategorii tutaj!
                // U�ytkownik b�dzie je mia� dopiero po dodaniu wydatk�w

                // Otw�rz g��wne okno aplikacji
                var shell = new ShellWindow();
                Application.Current.MainWindow = shell;
                shell.Show();

                //  Zamknij okno logowania
                Application.Current.Windows
                    .OfType<Finly.Views.AuthWindow>()
                    .FirstOrDefault()
                    ?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wyst�pi� b��d podczas logowania:\n{ex.Message}",
                    "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RaiseCanExecuteChanged()
        {
            if (LoginCommand is RelayCommand rc)
                rc.RaiseCanExecuteChanged();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
