using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Finly.Services;

namespace Finly.ViewModels
{
    public class AuthViewModel : INotifyPropertyChanged
    {
        // ===== Pola prywatne =====
        private string _username = string.Empty;
        private bool _isLoginMode = true;

        private string _registerMessage = string.Empty;
        private bool _registerIsError;

        private string _loginBanner = string.Empty;
        private Brush _loginBannerBrush = Brushes.Transparent;

        private string _loginMessage = string.Empty;
        private bool _loginIsError;

        // Regex e-maila (culture-invariant)
        private static readonly Regex EmailRegex =
            new(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // ===== Właściwości publiczne =====
        public string Username
        {
            get => _username;
            set
            {
                _username = (value ?? string.Empty).Trim();
                OnPropertyChanged();
                UpdateEmailValidity();
            }
        }

        public bool IsLoginMode
        {
            get => _isLoginMode;
            private set
            {
                if (_isLoginMode == value) return;
                _isLoginMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRegisterMode));
            }
        }
        public bool IsRegisterMode => !IsLoginMode;

        // Rejestracja – komunikat + kolor
        public string RegisterMessage
        {
            get => _registerMessage;
            set { _registerMessage = value ?? string.Empty; OnPropertyChanged(); }
        }
        public bool RegisterIsError
        {
            get => _registerIsError;
            set { _registerIsError = value; OnPropertyChanged(); OnPropertyChanged(nameof(RegisterMessageBrush)); }
        }
        public Brush RegisterMessageBrush => RegisterIsError ? Brushes.IndianRed : Brushes.SeaGreen;

        // Logowanie – baner u góry
        public string LoginBanner
        {
            get => _loginBanner;
            set { _loginBanner = value ?? string.Empty; OnPropertyChanged(); }
        }
        public Brush LoginBannerBrush
        {
            get => _loginBannerBrush;
            set { _loginBannerBrush = value ?? Brushes.Transparent; OnPropertyChanged(); }
        }

        // Logowanie – komunikat pod hasłem
        public string LoginMessage
        {
            get => _loginMessage;
            set { _loginMessage = value ?? string.Empty; OnPropertyChanged(); }
        }
        public bool LoginIsError
        {
            get => _loginIsError;
            set { _loginIsError = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoginMessageBrush)); }
        }
        public Brush LoginMessageBrush => LoginIsError ? Brushes.IndianRed : Brushes.SeaGreen;

        // Walidacja e-maila i aktywacja przycisku rejestracji
        public bool IsEmailValid { get; private set; }
        public bool CanRegister => IsEmailValid && IsPasswordValid;

        // Ustalany po poprawnym logowaniu
        public int LoggedInUserId { get; private set; } = -1;

        // ===== Przełączanie paneli =====
        public void SwitchToRegister()
        {
            IsLoginMode = false;
            ClearMessages();
            LoginBanner = string.Empty;
            LoginBannerBrush = Brushes.Transparent;
        }

        public void SwitchToLogin()
        {
            IsLoginMode = true;
            ClearMessages();
            LoginBanner = string.Empty;
            LoginBannerBrush = Brushes.Transparent;
        }

        public void ShowLogoutInfo()
        {
            IsLoginMode = true;
            Username = string.Empty;
            LoginBanner = "Zostałeś(-aś) wylogowany(-a).";
            LoginBannerBrush = Brushes.SteelBlue;
            ClearMessages();
        }

        public void ShowAccountDeletedInfo()
        {
            IsLoginMode = true;
            Username = string.Empty;
            LoginBanner = "Konto zostało usunięte.";
            LoginBannerBrush = Brushes.IndianRed;
            ClearMessages();
        }

        private void ClearMessages()
        {
            RegisterMessage = string.Empty;
            RegisterIsError = false;

            LoginMessage = string.Empty;
            LoginIsError = false;
        }

        private void UpdateEmailValidity()
        {
            IsEmailValid = EmailRegex.IsMatch(_username);
            OnPropertyChanged(nameof(IsEmailValid));
            OnPropertyChanged(nameof(CanRegister));
        }

        // ===== Logika: logowanie / rejestracja =====
        public bool Login(string password)
        {
            LoginMessage = string.Empty;
            LoginIsError = false;

            var u = (Username ?? string.Empty).Trim();
            if (!EmailRegex.IsMatch(u))
            {
                LoginIsError = true;
                LoginMessage = "Podaj poprawny adres e-mail.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                LoginIsError = true;
                LoginMessage = "Podaj hasło.";
                return false;
            }

            Username = u.ToLowerInvariant(); // kanonizacja
            var ok = UserService.Login(Username, password);
            if (!ok)
            {
                LoginIsError = true;
                LoginMessage = "Błędny login lub hasło.";
                return false;
            }

            LoggedInUserId = UserService.GetUserIdByUsername(Username);
            return LoggedInUserId > 0;
        }

        public bool Register(string password, string confirm)
        {
            ClearMessages();
            UpdateEmailValidity();
            UpdatePasswordHints(password, confirm);

            if (!IsEmailValid)
                return Error("Podaj poprawny adres e-mail.");

            if (!IsPasswordValid)
                return Error("Hasło nie spełnia wymagań – popraw czerwone pozycje poniżej.");

            Username = Username.ToLowerInvariant();

            if (!UserService.IsUsernameAvailable(Username))
                return Error("Nie udało się utworzyć konta. Login jest zajęty.");

            var ok = UserService.Register(Username, password);
            if (!ok)
                return Error("Nie udało się utworzyć konta. Spróbuj ponownie.");

            IsLoginMode = true;
            LoginBanner = "Konto utworzone. Zaloguj się.";
            LoginBannerBrush = Brushes.SeaGreen;
            return true;
        }

        // ===== Wymagania hasła (live) =====
        private bool _pwdHasMinLen, _pwdHasLower, _pwdHasUpper, _pwdHasDigit, _pwdHasSpecial, _pwdNoSpaces, _pwdMatchesConfirm;

        public bool PwdHasMinLen { get => _pwdHasMinLen; set { _pwdHasMinLen = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }
        public bool PwdHasLower { get => _pwdHasLower; set { _pwdHasLower = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }
        public bool PwdHasUpper { get => _pwdHasUpper; set { _pwdHasUpper = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }
        public bool PwdHasDigit { get => _pwdHasDigit; set { _pwdHasDigit = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }
        public bool PwdHasSpecial { get => _pwdHasSpecial; set { _pwdHasSpecial = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }
        public bool PwdNoSpaces { get => _pwdNoSpaces; set { _pwdNoSpaces = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }
        public bool PwdMatchesConfirm { get => _pwdMatchesConfirm; set { _pwdMatchesConfirm = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); } }

        public bool IsPasswordValid =>
            PwdHasMinLen && PwdHasLower && PwdHasUpper && PwdHasDigit && PwdHasSpecial && PwdNoSpaces && PwdMatchesConfirm;

        public void UpdatePasswordHints(string pwd, string confirm)
        {
            pwd ??= string.Empty; confirm ??= string.Empty;
            PwdHasMinLen = pwd.Length >= 8;
            PwdHasLower = Regex.IsMatch(pwd, "[a-z]");
            PwdHasUpper = Regex.IsMatch(pwd, "[A-Z]");
            PwdHasDigit = Regex.IsMatch(pwd, "[0-9]");
            PwdHasSpecial = Regex.IsMatch(pwd, "[^\\w\\s]");
            PwdNoSpaces = !Regex.IsMatch(pwd, "\\s");
            PwdMatchesConfirm = string.Equals(pwd, confirm, StringComparison.Ordinal);
        }

        // ===== Helpery =====
        private bool Error(string msg)
        {
            RegisterIsError = true;
            RegisterMessage = msg ?? string.Empty;
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
