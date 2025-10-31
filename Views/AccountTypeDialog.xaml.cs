using System.Windows;
using Finly.Models;

namespace Finly.Views
{
    public partial class AccountTypeDialog : Window
    {
        public AccountType Selected { get; private set; } = AccountType.Personal;

        public AccountTypeDialog()
        {
            InitializeComponent();
        }

        private void BtnPersonal_Click(object sender, RoutedEventArgs e)
        {
            Selected = AccountType.Personal;
            DialogResult = true;
        }

        private void BtnBusiness_Click(object sender, RoutedEventArgs e)
        {
            Selected = AccountType.Business;
            DialogResult = true;
        }
    }
}
