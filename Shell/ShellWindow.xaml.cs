using Finly.Pages;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace Finly.Shell
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            NavigateTo("Dashboard"); // start
        }

        public void NavigateTo(string tag)
        {
            switch (tag)
            {
                case "Dashboard": ContentHost.Content = new DashboardPage(); break;
                case "AddExpense": ContentHost.Content = new AddExpensePage(); break;
                case "Charts": ContentHost.Content = new ChartsPage(); break;
                case "Categories": ContentHost.Content = new CategoriesPage(); break;
                case "Settings": ContentHost.Content = new SettingsPage(); break;
                case "Logout": Close(); break;
                default: ContentHost.Content = new DashboardPage(); break;
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;
            NavigateTo(tag);
        }
    }
}
