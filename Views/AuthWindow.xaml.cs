using Finly.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Finly.Views
{
    public partial class AuthWindow : Window
    {
        private AuthViewModel VM => (AuthViewModel)DataContext;

        // --- stan do trybu fullscreen (bez ramek) ---
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private WindowState _prevState;

        public AuthWindow()
        {
            InitializeComponent();
            DataContext = new AuthViewModel(); // startowy VM
            // Nie wywołujemy EnterFullscreen(); okno i tak startuje zmaksymalizowane dzięki stylowi w App.xaml
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
                var dash = new DashboardView(VM.LoggedInUserId)
                {
                    WindowState = WindowState.Maximized,
                    ResizeMode = ResizeMode.CanResize
                };
                dash.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Błędny login lub hasło.", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // ====== Rejestracja ======
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var pwd = PwdRegText.Visibility == Visibility.Visible ? PwdRegText.Text : PwdReg.Password;
            var conf = PwdRegConfirmText.Visibility == Visibility.Visible ? PwdRegConfirmText.Text : PwdRegConfirm.Password;

            VM.UpdatePasswordHints(pwd, conf);

            if (VM.Register(pwd, conf))
            {
                // VM przełączył na login; schowaj jawne pola, jeżeli były aktywne
                RegShowPassword_Unchecked(null!, null!);
            }
        }

        // ====== Pokaż/ukryj hasło – LOGIN ======
        private void LoginShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            PwdLoginText.Text = PwdLogin.Password;
            PwdLogin.Visibility = Visibility.Collapsed;
            PwdLoginText.Visibility = Visibility.Visible;
            PwdLoginText.Focus();
            PwdLoginText.CaretIndex = PwdLoginText.Text.Length;
        }

        private void LoginShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            PwdLogin.Password = PwdLoginText.Text;
            PwdLoginText.Visibility = Visibility.Collapsed;
            PwdLogin.Visibility = Visibility.Visible;
            PwdLogin.Focus();
            PwdLogin.SelectAll();
        }

        // ====== Pokaż/ukryj hasło – REJESTRACJA ======
        private void RegShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            PwdRegText.Text = PwdReg.Password;
            PwdRegConfirmText.Text = PwdRegConfirm.Password;

            PwdReg.Visibility = Visibility.Collapsed;
            PwdRegConfirm.Visibility = Visibility.Collapsed;
            PwdRegText.Visibility = Visibility.Visible;
            PwdRegConfirmText.Visibility = Visibility.Visible;
        }

        private void RegShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            PwdReg.Password = PwdRegText.Text;
            PwdRegConfirm.Password = PwdRegConfirmText.Text;

            PwdRegText.Visibility = Visibility.Collapsed;
            PwdRegConfirmText.Visibility = Visibility.Collapsed;
            PwdReg.Visibility = Visibility.Visible;
            PwdRegConfirm.Visibility = Visibility.Visible;
        }

        // ====== Live walidacja hasła (rejestracja) ======
        private void PwdReg_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PwdRegText.Visibility != Visibility.Visible)
                VM.UpdatePasswordHints(PwdReg.Password, PwdRegConfirm.Password);
        }

        private void PwdRegConfirm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PwdRegConfirmText.Visibility != Visibility.Visible)
                VM.UpdatePasswordHints(PwdReg.Password, PwdRegConfirm.Password);
        }

        private void PwdRegText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PwdRegText.Visibility == Visibility.Visible)
                VM.UpdatePasswordHints(PwdRegText.Text, PwdRegConfirmText.Text);
        }

        private void PwdRegConfirmText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PwdRegConfirmText.Visibility == Visibility.Visible)
                VM.UpdatePasswordHints(PwdRegText.Text, PwdRegConfirmText.Text);
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
            WindowState = WindowState.Maximized; // start na cały ekran
            ResizeMode = ResizeMode.CanResize;  // pozwól zmieniać rozmiar / maksymalizować
        }


    }
}
