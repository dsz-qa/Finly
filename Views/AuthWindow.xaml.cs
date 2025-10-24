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
        // Jeden, typowany skrót do VM (usunięta wcześniejsza dublująca definicja)
        private AuthViewModel VM => (AuthViewModel)DataContext;

        // --- stan do trybu fullscreen (bez ramek) ---
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private WindowState _prevState;

        public AuthWindow()
        {
            InitializeComponent();
            DataContext = new AuthViewModel(); // startowy VM
            // Okno startuje zmaksymalizowane dzięki stylowi w App.xaml
        }

        // ===== Pasek tytułu: przeciąganie + przyciski =====
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Jeśli klik na przycisku – nie przeciągamy
            if (IsInside<Button>(e.OriginalSource as DependencyObject))
                return;

            // Double–click na pasku = max / restore
            if (e.ClickCount == 2)
            {
                MaxRestore_Click(sender, e);
                return;
            }

            try { DragMove(); } catch { /* ignoruj sporadyczny InvalidOperation */ }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaxRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // (opcjonalnie) delikatny hover dla X
        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (CloseGlyph != null) CloseGlyph.Foreground = Brushes.IndianRed;
        }
        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (CloseGlyph != null) CloseGlyph.Foreground = Brushes.WhiteSmoke;
        }

        // ===== Pomocnicze: czy źródło eventu jest wewnątrz danego typu (np. Button)?
        private static bool IsInside<T>(DependencyObject? src) where T : DependencyObject
        {
            while (src != null)
            {
                if (src is T) return true;
                src = VisualTreeHelper.GetParent(src);
            }
            return false;
        }

        // ====== Przełączanie paneli ======
        private void SwitchToRegister_Click(object sender, RoutedEventArgs e) => VM.SwitchToRegister();
        private void SwitchToLogin_Click(object sender, RoutedEventArgs e) => VM.SwitchToLogin();

        // ====== Logowanie ======
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
                // NIC nie pokazujemy – VM ustawił LoginIsError + LoginMessage.
                // Przesuń focus na login, żeby użytkownik od razu mógł poprawić.
                LoginUsername.Focus();
                LoginUsername.SelectAll();
            }
        }

        // --- helper: reset komunikatu błędu logowania ---
        private void ClearLoginError()
        {
            VM.LoginIsError = false;
            VM.LoginMessage = string.Empty;
        }

        // Wywoływane, gdy wpisujesz login
        private void LoginUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearLoginError();
        }

        // Wywoływane, gdy wpisujesz hasło w PasswordBox (tryb ukryty)
        private void PwdLogin_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ClearLoginError();
        }

        // Wywoływane, gdy wpisujesz hasło w TextBox (tryb „Pokaż hasło”)
        private void PwdLoginText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearLoginError();
        }

        private void OnLoginSuccess(int userId)
        {
            UserService.CurrentUserId = userId;

            var shell = new ShellWindow();
            Application.Current.MainWindow = shell;
            shell.Show();

            Close();
        }

        // ====== Rejestracja ======
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var pwd = PwdRegText.Visibility == Visibility.Visible ? PwdRegText.Text : PwdReg.Password;
            var conf = PwdRegConfirmText.Visibility == Visibility.Visible ? PwdRegConfirmText.Text : PwdRegConfirm.Password;

            VM.UpdatePasswordHints(pwd, conf);

            if (VM.Register(pwd, conf))
            {
                // po założeniu konta i przełączeniu na login – schowaj ewentualne jawne pola
                RegShowPassword_Unchecked(null!, null!);
            }
        }

        // ====== Pokaż/ukryj hasło – LOGIN (eventy) ======
        private void LoginShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            // przepisz zawartość i pokaż TextBox
            PwdLoginText.Text = PwdLogin.Password;

            // ustaw widoczności i upewnij się, że TextBox jest na wierzchu
            PwdLogin.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(PwdLogin, 0);

            PwdLoginText.Visibility = Visibility.Visible;
            Panel.SetZIndex(PwdLoginText, 1);

            // fokus i kursor na koniec
            PwdLoginText.Focus();
            PwdLoginText.CaretIndex = PwdLoginText.Text.Length;
        }

        private void LoginShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            // przepisz z powrotem do PasswordBox
            PwdLogin.Password = PwdLoginText.Text ?? string.Empty;

            // przełącz widoczność i „warstwę”
            PwdLoginText.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(PwdLoginText, 0);

            PwdLogin.Visibility = Visibility.Visible;
            Panel.SetZIndex(PwdLogin, 1);

            // fokus do PasswordBox
            PwdLogin.Focus();
            PwdLogin.SelectAll();
        }


        // ====== Pokaż/ukryj hasło – REJESTRACJA (synchronizacja) ======
        // Gdy checkbox w rejestracji zmienia widoczność pól, te handlery łączą ich treść z VM
        // ====== Pokaż/ukryj hasło – REJESTRACJA (synchronizacja) ======
        // Flaga anty-rekurencyjna (chroni przed zapętleniem, gdy przepisujemy tekst programowo)
        private bool _syncingRegPasswords = false;

        // pomocnicze: ustawienie tekstu w TextBox tylko gdy trzeba (z zachowaniem kursora)
        private static void SetTextIfDifferent(TextBox tb, string value)
        {
            if (tb.Text == value) return;
            tb.Text = value ?? string.Empty;
            tb.CaretIndex = tb.Text.Length;
        }

        // pomocnicze: ustawienie hasła w PasswordBox tylko gdy trzeba
        private static void SetPasswordIfDifferent(PasswordBox pb, string value)
        {
            if (pb.Password == (value ?? string.Empty)) return;
            pb.Password = value ?? string.Empty;
        }

        // === Hasło (główne) ===
        private void PwdReg_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingRegPasswords) return;
            _syncingRegPasswords = true;

            var val = ((PasswordBox)sender).Password;
            if (VM.Password != val) VM.Password = val;

            // jeśli widoczny TextBox, trzymaj z nim zgodność
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

            // jeśli ukryty PasswordBox jest aktywny, trzymaj zgodność
            if (PwdReg.Visibility == Visibility.Visible)
                SetPasswordIfDifferent(PwdReg, val);

            _syncingRegPasswords = false;
        }

        // === Powtórz hasło ===
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


        // Po włączeniu "Pokaż hasło" – przepisz z PasswordBox -> TextBox
        private void RegShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            // hasło główne
            if (PwdRegText != null && PwdReg != null)
                PwdRegText.Text = PwdReg.Password;

            // powtórz hasło
            if (PwdRegConfirmText != null && PwdRegConfirm != null)
                PwdRegConfirmText.Text = PwdRegConfirm.Password;
        }

        // Po wyłączeniu "Pokaż hasło" – przepisz z TextBox -> PasswordBox
        private void RegShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            // hasło główne
            if (PwdRegText != null && PwdReg != null)
                PwdReg.Password = PwdRegText.Text ?? string.Empty;

            // powtórz hasło
            if (PwdRegConfirmText != null && PwdRegConfirm != null)
                PwdRegConfirm.Password = PwdRegConfirmText.Text ?? string.Empty;
        }


        // ====== Skróty klawiatury ======
        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) ToggleFullscreen();
            if (e.Key == Key.Escape) Close();
        }

        // ====== Fullscreen helpers ======
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

