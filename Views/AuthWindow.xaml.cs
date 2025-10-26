using Finly.Services;
using Finly.Shell;
using Finly.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Finly.Views
{
    public partial class AuthWindow : Window
    {
        private AuthViewModel VM => (AuthViewModel)DataContext;

        // --- stan fullscreena ---
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private WindowState _prevState;

        public AuthWindow()
        {
            InitializeComponent();
            DataContext = new AuthViewModel(); // startowy VM
            // Styl okna w App.xaml wymusza Maximize + rozmiary
        }

        // ===== Pasek tytułu =====
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsInside<Button>(e.OriginalSource as DependencyObject))
                return;

            if (e.ClickCount == 2)
            {
                MaxRestore_Click(sender, e);
                return;
            }

            try { DragMove(); } catch { /* ignoruj sporadyczny InvalidOperation */ }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaxRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // delikatny hover dla X (opcjonalnie – jeśli w XAML masz CloseGlyph)
        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (CloseGlyph != null) CloseGlyph.Foreground = Brushes.IndianRed;
        }
        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (CloseGlyph != null) CloseGlyph.Foreground = Brushes.WhiteSmoke;
        }

        // helper: czy event pochodzi z danego typu
        private static bool IsInside<T>(DependencyObject? src) where T : DependencyObject
        {
            while (src != null)
            {
                if (src is T) return true;
                src = VisualTreeHelper.GetParent(src);
            }
            return false;
        }

        // ===== Przełączanie paneli (login/registracja) =====
        private void SwitchToRegister_Click(object sender, RoutedEventArgs e) => VM.SwitchToRegister();
        private void SwitchToLogin_Click(object sender, RoutedEventArgs e) => VM.SwitchToLogin();

        // ===== Logowanie =====
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var pwd = PwdLoginText.Visibility == Visibility.Visible ? PwdLoginText.Text : PwdLogin.Password;

            if (VM.Login(pwd))
            {
                var userId = VM.LoggedInUserId != 0 ? VM.LoggedInUserId : UserService.GetUserIdByUsername(VM.Username);
                OnLoginSuccess(userId);
            }
            else
            {
                LoginUsername.Focus();
                LoginUsername.SelectAll();
            }
        }

        private void ClearLoginError()
        {
            VM.LoginIsError = false;
            VM.LoginMessage = string.Empty;
        }

        private void LoginUsername_TextChanged(object sender, TextChangedEventArgs e) => ClearLoginError();
        private void PwdLogin_PasswordChanged(object sender, RoutedEventArgs e) => ClearLoginError();
        private void PwdLoginText_TextChanged(object sender, TextChangedEventArgs e) => ClearLoginError();

        private void OnLoginSuccess(int userId)
        {
            UserService.CurrentUserId = userId;

            var shell = new ShellWindow();
            Application.Current.MainWindow = shell;
            shell.Show();
            Close();
        }

        // ===== Rejestracja =====
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var pwd = PwdRegText.Visibility == Visibility.Visible ? PwdRegText.Text : PwdReg.Password;
            var conf = PwdRegConfirmText.Visibility == Visibility.Visible ? PwdRegConfirmText.Text : PwdRegConfirm.Password;

            VM.UpdatePasswordHints(pwd, conf);

            if (VM.Register(pwd, conf))
            {
                // schowaj ewentualne jawne pola
                RegShowPassword_Unchecked(null!, null!);
            }
        }

        // ===== Pokaż/ukryj hasło – LOGIN =====
        private void LoginShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            PwdLoginText.Text = PwdLogin.Password;

            PwdLogin.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(PwdLogin, 0);

            PwdLoginText.Visibility = Visibility.Visible;
            Panel.SetZIndex(PwdLoginText, 1);

            PwdLoginText.Focus();
            PwdLoginText.CaretIndex = PwdLoginText.Text.Length;
        }

        private void LoginShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            PwdLogin.Password = PwdLoginText.Text ?? string.Empty;

            PwdLoginText.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(PwdLoginText, 0);

            PwdLogin.Visibility = Visibility.Visible;
            Panel.SetZIndex(PwdLogin, 1);

            PwdLogin.Focus();
            PwdLogin.SelectAll();
        }

        // ===== Pokaż/ukryj hasło – REJESTRACJA =====
        private bool _syncingRegPasswords = false;

        private static void SetTextIfDifferent(TextBox tb, string value)
        {
            if (tb.Text == value) return;
            tb.Text = value ?? string.Empty;
            tb.CaretIndex = tb.Text.Length;
        }

        private static void SetPasswordIfDifferent(PasswordBox pb, string value)
        {
            if (pb.Password == (value ?? string.Empty)) return;
            pb.Password = value ?? string.Empty;
        }

        private void PwdReg_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingRegPasswords) return;
            _syncingRegPasswords = true;

            var val = ((PasswordBox)sender).Password;
            if (VM.Password != val) VM.Password = val;

            if (PwdRegText.Visibility == Visibility.Visible)
                SetTextIfDifferent(PwdRegText, val);

            _syncingRegPasswords = false;
        }

        private void PwdRegText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncingRegPasswords) return;
            _syncingRegPasswords = true;

            var val = ((TextBox)sender).Text;
            if (VM.Password != val) VM.Password = val;

            if (PwdReg.Visibility == Visibility.Visible)
                SetPasswordIfDifferent(PwdReg, val);

            _syncingRegPasswords = false;
        }

        private void PwdRegConfirm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingRegPasswords) return;
            _syncingRegPasswords = true;

            var val = ((PasswordBox)sender).Password;
            if (VM.RepeatPassword != val) VM.RepeatPassword = val;

            if (PwdRegConfirmText.Visibility == Visibility.Visible)
                SetTextIfDifferent(PwdRegConfirmText, val);

            _syncingRegPasswords = false;
        }

        private void PwdRegConfirmText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncingRegPasswords) return;
            _syncingRegPasswords = true;

            var val = ((TextBox)sender).Text;
            if (VM.RepeatPassword != val) VM.RepeatPassword = val;

            if (PwdRegConfirm.Visibility == Visibility.Visible)
                SetPasswordIfDifferent(PwdRegConfirm, val);

            _syncingRegPasswords = false;
        }

        private void RegShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            if (PwdRegText != null && PwdReg != null)
                PwdRegText.Text = PwdReg.Password;

            if (PwdRegConfirmText != null && PwdRegConfirm != null)
                PwdRegConfirmText.Text = PwdRegConfirm.Password;
        }

        private void RegShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PwdRegText != null && PwdReg != null)
                PwdReg.Password = PwdRegText.Text ?? string.Empty;

            if (PwdRegConfirmText != null && PwdRegConfirm != null)
                PwdRegConfirm.Password = PwdRegConfirmText.Text ?? string.Empty;
        }

        // ===== Skróty klawiatury / fullscreen =====
        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) ToggleFullscreen();
            if (e.Key == Key.Escape) Close();
        }

        private void EnterFullscreen()
        {
            _prevStyle = WindowStyle;
            _prevResize = ResizeMode;
            _prevState = WindowState;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }

        private void ExitFullscreen()
        {
            WindowStyle = _prevStyle;
            ResizeMode = _prevResize;
            WindowState = _prevState == WindowState.Minimized ? WindowState.Normal : _prevState;
        }

        private void ToggleFullscreen()
        {
            if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized)
                ExitFullscreen();
            else
                EnterFullscreen();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResize;
        }
    }
}


