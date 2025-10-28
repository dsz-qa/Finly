using Finly.Pages;
using Finly.Services;
using Finly.Views;
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
            NavigateToDashboard(); // start na stronie głównej
        }

        public void NavigateTo(string route)
            {
                // Jeśli nie jesteśmy zalogowani, wróć do loginu
                var uid = UserService.CurrentUserId;
                if (uid == 0)
                {
                    RightHost.Content = new LoginView(); // jeśli masz; w przeciwnym razie wyjdź:
                    return;
                }

                UserControl view = route switch
                {
                    // główne
                    "Dashboard" => new DashboardPage(uid),
                    "AddExpense" => new AddExpensePage(uid),
                    "Transactions" => new TransactionsPage(uid),
                    "Budgets" => new BudgetsPage(uid),
                    "Categories" => new CategoriesPage(uid),
                    "Reports" => new ReportsPage(uid),
                    "Subscriptions" => new SubscriptionsPage(uid),
                    "Goals" => new GoalsPage(uid),
                    "Charts" => new ChartsPage(uid),

                    // narzędzia / integracje
                    "Import" => new ImportSyncPage("banks"),
                    "Settings" => new SettingsPage(null),

                    _ => new DashboardPage(uid)
                };

                RightHost.Content = view;   // RightHost to Twój ContentControl w ShellWindow.xaml
            }

    // ===== Pasek tytułu =====
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { MaxRestore_Click(sender, e); return; }
            try { DragMove(); } catch { }
        }
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaxRestore_Click(object s, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object s, RoutedEventArgs e) => Close();

        // ===== Top bar: konto / ustawienia / wyloguj =====
        private void AccountButton_Click(object s, RoutedEventArgs e)
        {
            // uzupełnij danymi
            var info = new
            {
                DisplayName = UserService.CurrentUserName ?? "Użytkownik",
                Email = UserService.CurrentUserEmail ?? "—"
            };
            AccountMenu.DataContext = info;
            AccountMenu.PlacementTarget = AccountButton;
            AccountMenu.IsOpen = true;
        }

        private void OpenProfile_Click(object s, RoutedEventArgs e) => NavigateToSettings("Profile");
        private void OpenBanks_Click(object s, RoutedEventArgs e) => NavigateToImportSync("Banks");
        private void OpenSecurity_Click(object s, RoutedEventArgs e) => NavigateToSettings("Security");
        private void OpenBackups_Click(object s, RoutedEventArgs e) => NavigateToSettings("Backups");
        private void OpenSettings_Click(object s, RoutedEventArgs e) => NavigateToSettings(null);

        public void Nav_Logout_Click(object s, RoutedEventArgs e)
        {
            // Twój istniejący mechanizm wylogowania:
            // np. otwarcie AuthWindow i zamknięcie ShellWindow
            var auth = new Finly.Views.AuthWindow();
            Application.Current.MainWindow = auth;
            auth.Show();
            Close();
        }

        // ===== Logo = dashboard =====
        private void Logo_Click(object s, RoutedEventArgs e) => NavigateToDashboard();

        private void NavigateToDashboard()
        {
            int uid = UserService.CurrentUserId;
            RightHost.Content = new DashboardPage(uid);
            SetActiveNav(null); // nic niepodświetlone w sidebarze
        }

        private void NavigateToSettings(string? tabKey)
        {
            RightHost.Content = new SettingsPage(tabKey);
            SetActiveNav(NavSettings);
        }
        private void NavigateToImportSync(string? sectionKey)
        {
            RightHost.Content = new ImportSyncPage(sectionKey);
            SetActiveNav(NavImport);
        }

        // ===== Nawigacja sidebara =====
        private void Nav_Add_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new AddExpensePage(UserService.CurrentUserId);
            SetActiveNav(NavAdd);
        }
        private void Nav_Transactions_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new TransactionsPage(UserService.CurrentUserId);
            SetActiveNav(NavTransactions);
        }
        private void Nav_Charts_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ChartsPage(UserService.CurrentUserId);
            SetActiveNav(NavCharts);
        }
        private void Nav_Budgets_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new BudgetsPage(UserService.CurrentUserId);
            SetActiveNav(NavBudgets);
        }
        private void Nav_Subscriptions_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new SubscriptionsPage(UserService.CurrentUserId);
            SetActiveNav(NavSubscriptions);
        }
        private void Nav_Goals_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new GoalsPage(UserService.CurrentUserId);
            SetActiveNav(NavGoals);
        }
        private void Nav_Categories_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new CategoriesPage(UserService.CurrentUserId);
            SetActiveNav(NavCategories);
        }
        private void Nav_Reports_Click(object s, RoutedEventArgs e)
        {
            RightHost.Content = new ReportsPage(UserService.CurrentUserId);
            SetActiveNav(NavReports);
        }
        private void Nav_Import_Click(object s, RoutedEventArgs e)
        {
            NavigateToImportSync(null);
        }

        private void SetActiveNav(ToggleButton? active)
        {
            foreach (var child in NavContainer.Children)
                if (child is ToggleButton tb) tb.IsChecked = (tb == active);
        }
    }
}
