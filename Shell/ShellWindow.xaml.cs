using Finly.Pages;   // DashboardPage, AddExpensePage, ChartsPage, CategoriesPage, SettingsPage
using Finly.Services;     // UserService
using Finly.ViewModels;   // AuthViewModel
using Finly.Views;        // AuthWindow
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


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
        private void Nav_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActive(NavDashboard);
            NavigateTo("Dashboard");
        }

        private void Nav_Add_Click(object sender, RoutedEventArgs e)
        {
            SetActive(NavAdd);
            NavigateTo("AddExpense");
        }

        private void Nav_Charts_Click(object sender, RoutedEventArgs e)
        {
            SetActive(NavCharts);
            NavigateTo("Charts");
        }

        private void Nav_Categories_Click(object sender, RoutedEventArgs e)
        {
            SetActive(NavCategories);
            NavigateTo("Categories");
        }

        private void Nav_Settings_Click(object sender, RoutedEventArgs e)
        {
            SetActive(NavSettings);
            NavigateTo("Settings");
        }

        private void Nav_Logout_Click(object sender, RoutedEventArgs e)
        {
            // jak dotychczas – powrót do AuthWindow
            Logout_Click(sender, e);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // wyczyść sesję/logowanie
            UserService.CurrentUserId = 0;

            // utwórz okno logowania i ustaw jako główne
            var auth = new AuthWindow();
            Application.Current.MainWindow = auth;

            // pokaż niebieski baner "Zostałeś(-aś) wylogowany(-a)."
            if (auth.DataContext is AuthViewModel vm)
                vm.ShowLogoutInfo();

            auth.Show();

            // zamknij powłokę (NIE zamyka aplikacji przy ShutdownMode=OnMainWindowClose)
            this.Close();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                MaxRestore_Click(sender, e);
            else
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaxRestore_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();

        private void SetActive(params ToggleButton[] toActivate)
        {
            // odznacz wszystkie ToggleButtony z kontenera nawigacji
            foreach (var child in NavContainer.Children)
                if (child is ToggleButton tb) tb.IsChecked = false;

            foreach (var tb in toActivate) tb.IsChecked = true;
        }


    }
}
