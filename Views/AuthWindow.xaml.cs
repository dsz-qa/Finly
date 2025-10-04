using System.Windows;
using System.Windows.Controls;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class AuthWindow : Window
    {
        private AuthViewModel VM => (AuthViewModel)DataContext;

        public AuthWindow()
        {
            InitializeComponent();
            DataContext = new AuthViewModel();
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
                var dash = new DashboardView(VM.LoggedInUserId);
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
                // VM przełącza na login; schowaj jawne pola jeśli były włączone
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
    }
}
