using System.Windows;
using System.Windows.Controls;
using Finly.ViewModels;

namespace Finly.Pages
{
    public partial class AccountPage : UserControl
    {
        private readonly AccountViewModel _vm;

        public AccountPage(int userId)
        {
            InitializeComponent();
            _vm = new AccountViewModel(userId);
            DataContext = _vm;
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var old = PwdOld?.Password ?? string.Empty;
            var n1 = PwdNew?.Password ?? string.Empty;
            var n2 = PwdNew2?.Password ?? string.Empty;

            _vm.ChangePassword(old, n1, n2);

            PwdOld.Password = string.Empty;
            PwdNew.Password = string.Empty;
            PwdNew2.Password = string.Empty;
        }
    }
}
