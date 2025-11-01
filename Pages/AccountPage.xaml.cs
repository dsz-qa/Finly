using System.Windows;
using System.Windows.Controls;
using Finly.Services;
using Finly.ViewModels;

namespace Finly.Pages
{
    public partial class AccountPage : UserControl
    {
        private readonly int _userId;
        private AccountViewModel _vm;

        // Dla designera / nawigacji bez przekazywania parametru
        public AccountPage() : this(UserService.GetCurrentUserId()) { }

        public AccountPage(int userId)
        {
            InitializeComponent();
            _userId = userId <= 0 ? UserService.GetCurrentUserId() : userId;
            _vm = new AccountViewModel(_userId);
            DataContext = _vm;
        }

        // PRZYCISK: „Uzupełnij dane osobowe” (lub „Edytuj dane”)
        private void EditPersonal_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Finly.Views.EditPersonalWindow(_userId)   // <— przekazujemy userId
            {
                Owner = Application.Current.MainWindow,
                DataContext = new EditPersonalViewModel(_userId)     // zostaw, jeśli okno nie ustawia samo
            };

            var ok = dlg.ShowDialog() == true;
            if (ok)
            {
                _vm = new AccountViewModel(_userId);
                DataContext = _vm;
            }
        }


        // PRZYCISK: „Zmień hasło”
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AccountViewModel vm) return;

            // PwdOld / PwdNew / PwdNew2 masz nazwane w XAML
            var oldPwd = PwdOld.Password;
            var newPwd = PwdNew.Password;
            var newPwd2 = PwdNew2.Password;

            vm.ChangePassword(oldPwd, newPwd, newPwd2);

            // Opcjonalnie czyścimy pola:
            // PwdOld.Password = PwdNew.Password = PwdNew2.Password = string.Empty;
        }
    }
}
