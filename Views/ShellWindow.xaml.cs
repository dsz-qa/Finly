using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Finly.Pages;
using Finly.Services;
using Finly.Views;

namespace Finly.Shell
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            ApplySidebarWidthFromResource();   // Ustaw szerokość z resource (jeśli jest)
            NavigateTo("dashboard");
        }

        // ======================= Nawigacja główna =======================
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

        // ======================= Pasek tytułu =======================
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { MaxRestore_Click(sender, e); return; }
            try { DragMove(); } catch { /* ignoruj */ }
        }

        private void Minimize_Click(object s, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void MaxRestore_Click(object s, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void Close_Click(object s, RoutedEventArgs e) => Close();

        // ======================= Logo => Dashboard =======================
        private void Logo_Click(object s, RoutedEventArgs e) => NavigateToDashboard();

        private void NavigateToDashboard()
        {
            var uid = UserService.CurrentUserId;
            RightHost.Content = new DashboardPage(uid);
            SetActiveNav(null);
            SetActiveFooter(null);
        }

        // ======================= Sidebar (lewa nawigacja) =======================
        private void Nav_Add_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new AddExpensePage(UserService.CurrentUserId);
            SetActiveNav(NavAdd);
            SetActiveFooter(null);
        }

        private void Nav_Transactions_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new TransactionsPage();
            SetActiveNav(NavTransactions);
            SetActiveFooter(null);
        }

        private void Nav_Charts_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ChartsPage(UserService.CurrentUserId);
            SetActiveNav(NavCharts);
            SetActiveFooter(null);
        }

        private void Nav_Budgets_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new BudgetsPage();
            SetActiveNav(NavBudgets);
            SetActiveFooter(null);
        }

        private void Nav_Subscriptions_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new SubscriptionsPage();
            SetActiveNav(NavSubscriptions);
            SetActiveFooter(null);
        }

        private void Nav_Goals_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new GoalsPage();
            SetActiveNav(NavGoals);
            SetActiveFooter(null);
        }

        private void Nav_Categories_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new CategoriesPage();
            SetActiveNav(NavCategories);
            SetActiveFooter(null);
        }

        private void Nav_Reports_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ReportsPage();
            SetActiveNav(NavReports);
            SetActiveFooter(null);
        }

        private void Nav_Import_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ImportPage();
            SetActiveNav(NavImport);
            SetActiveFooter(null);
        }

        // ======================= Stopka (Konto / Ustawienia / Wyloguj) =======================
        private void OpenProfile_Click(object s, RoutedEventArgs e)
        {
            NavigateTo("settings");   // profil w sekcji ustawień
            SetActiveNav(null);
            SetActiveFooter(FooterAccount);
        }

        private void OpenSettings_Click(object s, RoutedEventArgs e)
        {
            NavigateTo("settings");
            SetActiveNav(null);
            SetActiveFooter(FooterSettings);
        }

        public void Nav_Logout_Click(object s, RoutedEventArgs e)
        {
            var auth = new AuthWindow();
            Application.Current.MainWindow = auth;
            auth.Show();
            Close();
        }

        // ======================= Podświetlenia (helpery) =======================
        private void SetActiveNav(ToggleButton? active)
        {
            foreach (var child in NavContainer.Children)
            {
                if (child is ToggleButton tb)
                    tb.IsChecked = (tb == active);
            }
        }

        private void SetActiveFooter(ToggleButton? active)
        {
            if (FooterAccount != null) FooterAccount.IsChecked = active == FooterAccount;
            if (FooterSettings != null) FooterSettings.IsChecked = active == FooterSettings;
            if (FooterLogout != null) FooterLogout.IsChecked = false; // Logout nigdy nie zostaje aktywny
        }

        // ======================= Szerokość sidebara z Resource =======================
        private void ApplySidebarWidthFromResource()
        {
            try
            {
                var res = TryFindResource("Sidebar.Width");
                if (res is string s && double.TryParse(s, out var w))
                    SidebarCol.Width = new GridLength(w);
                else if (res is double d)
                    SidebarCol.Width = new GridLength(d);
                // brak zasobu -> zostaje fallback 420 z XAML
            }
            catch
            {
                // cicho ignorujemy – zostaje fallback
            }
        }
    }
}
