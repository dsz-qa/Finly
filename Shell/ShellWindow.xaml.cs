using System.Windows;
using System.Windows.Controls;
using Finly.Pages;   // DashboardPage, AddExpensePage, ChartsPage, CategoriesPage, SettingsPage

namespace Finly.Shell
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            NavigateTo("Dashboard");
        }

        public void NavigateTo(string target)
        {
            UserControl view = target switch
            {
                "Dashboard" => new DashboardPage(),
                "AddExpense" => new AddExpensePage(),
                "Charts" => new ChartsPage(),
                "Categories" => new CategoriesPage(),
                "Settings" => new SettingsPage(),
                _ => new DashboardPage()
            };

            RightHost.Content = view;   // << nazwa zgodna z XAML
        }

        // --- GÓRNE MENU ---
        private void Nav_Dashboard_Click(object sender, RoutedEventArgs e) => NavigateTo("Dashboard");
        private void Nav_Add_Click(object sender, RoutedEventArgs e) => NavigateTo("AddExpense");
        private void Nav_Charts_Click(object sender, RoutedEventArgs e) => NavigateTo("Charts");
        private void Nav_Categories_Click(object sender, RoutedEventArgs e) => NavigateTo("Categories");
        private void Nav_Settings_Click(object sender, RoutedEventArgs e) => NavigateTo("Settings");
        private void Nav_Logout_Click(object sender, RoutedEventArgs e)
        {
            // tutaj Twoja logika wylogowania/otwarcia AuthWindow
            Close();
        }
    }
}
