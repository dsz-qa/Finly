using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

using Finly.Services;
using Finly.Pages;
using Finly.Views;

namespace Finly.Shell
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            NavigateTo("dashboard");
        }

        public void NavigateTo(string route)
        {
            var uid = UserService.CurrentUserId;
            if (uid <= 0)
            {
                var auth = new AuthWindow();
                Application.Current.MainWindow = auth;
                auth.Show();
                Close();
                return;
            }

            UserControl view = (route ?? string.Empty).ToLowerInvariant() switch
            {
                "dashboard" => new DashboardPage(uid),
                "addexpense" => new AddExpensePage(uid),
                "transactions" => new TransactionsPage(),
                "budget" or "budgets" => new BudgetsPage(),
                "categories" => new CategoriesPage(),
                "subscriptions" => new SubscriptionsPage(),
                "goals" => new GoalsPage(),
                "charts" => new ChartsPage(uid),
                "import" => new ImportPage(),
                "reports" => new ReportsPage(),
                "settings" => new SettingsPage(),
                _ => new DashboardPage(uid),
            };

            RightHost.Content = view;
        }

        // ===== Pasek tytułu =====
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { MaxRestore_Click(sender, e); return; }
            try { DragMove(); } catch { }
        }

        private void Minimize_Click(object s, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void MaxRestore_Click(object s, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void Close_Click(object s, RoutedEventArgs e) => Close();

        // ===== Akcje konta (na dole sidebara) =====
        private void OpenProfile_Click(object s, RoutedEventArgs e)
        {
            NavigateTo("settings");
            SetActiveNav(null);
        }

        private void OpenSettings_Click(object s, RoutedEventArgs e)
        {
            NavigateTo("settings");
            SetActiveNav(null);
        }

        public void Nav_Logout_Click(object s, RoutedEventArgs e)
        {
            var auth = new AuthWindow();
            Application.Current.MainWindow = auth;
            auth.Show();
            Close();
        }

        // ===== Logo = dashboard =====
        private void Logo_Click(object s, RoutedEventArgs e) => NavigateToDashboard();

        private void NavigateToDashboard()
        {
            var uid = UserService.CurrentUserId;
            RightHost.Content = new DashboardPage(uid);
            SetActiveNav(null);
        }

        // ===== Nawigacja sidebara =====
        private void Nav_Add_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new AddExpensePage(UserService.CurrentUserId);
            SetActiveNav(NavAdd);
        }

        private void Nav_Transactions_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new TransactionsPage();
            SetActiveNav(NavTransactions);
        }

        private void Nav_Charts_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ChartsPage(UserService.CurrentUserId);
            SetActiveNav(NavCharts);
        }

        private void Nav_Budgets_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new BudgetsPage();
            SetActiveNav(NavBudgets);
        }

        private void Nav_Subscriptions_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new SubscriptionsPage();
            SetActiveNav(NavSubscriptions);
        }

        private void Nav_Goals_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new GoalsPage();
            SetActiveNav(NavGoals);
        }

        private void Nav_Categories_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new CategoriesPage();
            SetActiveNav(NavCategories);
        }

        private void Nav_Reports_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ReportsPage();
            SetActiveNav(NavReports);
        }

        private void Nav_Import_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ImportPage();
            SetActiveNav(NavImport);
        }

        private void SetActiveNav(ToggleButton? active)
        {
            foreach (var child in NavContainer.Children)
                if (child is ToggleButton tb)
                    tb.IsChecked = (tb == active);
        }
    }
}
