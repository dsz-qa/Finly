using System.Windows;
using System.Windows.Controls;
using Finly.Pages;

namespace Finly.Shell
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            NavigateTo("Dashboard");
        }

        public void NavigateTo(string tag)
        {
            switch (tag)
            {
                case "Dashboard": ContentHost.Content = new DashboardPage(); break;
                case "AddExpense": ContentHost.Content = new Finly.Pages.AddExpensePage(); break;
                case "Charts": ContentHost.Content = new Finly.Pages.ChartsPage(); break;
                case "Categories": ContentHost.Content = new Finly.Pages.CategoriesPage(); break;
                case "Settings": ContentHost.Content = new Finly.Pages.SettingsPage(); break;
                case "Logout": Close(); break;
                default: ContentHost.Content = new DashboardPage(); break;
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tag) NavigateTo(tag);
        }
    }
}
