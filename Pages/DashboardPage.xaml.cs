using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;

using Finly.Models;
using Finly.Services;      // DatabaseService, SchemaService, ToastService, UserService
using Finly.ViewModels;
using Finly.Views;         // AuthWindow, EditExpenseView, ConfirmDialog

namespace Finly.Pages
{
    public partial class DashboardPage : UserControl
    {
        private readonly int _userId;
        private List<ExpenseDisplayModel> _expenses = new();

        // Próba pobrania aktualnego userId z serwisu
        public DashboardPage() : this(GetCurrentUserIdSafe()) { }

        public DashboardPage(int userId)
        {
            InitializeComponent();
            _userId = userId;

            LoadExpenses();
            LoadCategories();
        }

        private static int GetCurrentUserIdSafe()
        {
            try { return UserService.GetCurrentUserId(); }
            catch { return 0; }
        }

        // ================== DOLNY PASEK – NAWIGACJA ==================

        private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            (Window.GetWindow(this) as Finly.Shell.ShellWindow)?.NavigateTo("AddExpense");
        }

        private void ShowChart_Click(object sender, RoutedEventArgs e)
        {
            (Window.GetWindow(this) as Finly.Shell.ShellWindow)?.NavigateTo("Charts");
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ConfirmDialog(
                "Na pewno chcesz trwale usunąć konto wraz ze wszystkimi wydatkami i kategoriami?\n" +
                "Tej operacji nie można cofnąć.");
            dlg.Owner = Window.GetWindow(this);

            if (dlg.ShowDialog() == true && dlg.Result)
            {
                try
                {
                    if (!UserService.DeleteAccount(_userId))
                    {
                        ToastService.Error("Nie udało się usunąć konta.");
                        return;
                    }

                    ToastService.Success("Konto zostało usunięte.");

                    // Zamknij Shell i wróć do logowania
                    var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
                    shell?.Close();

                    var auth = new AuthWindow();
                    if (auth.DataContext is AuthViewModel vm)
                        vm.ShowAccountDeletedInfo();

                    Application.Current.MainWindow = auth;
                    auth.Show();
                }
                catch (Exception ex)
                {
                    ToastService.Error("Błąd podczas usuwania konta: " + ex.Message);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var auth = new AuthWindow();
            if (auth.DataContext is AuthViewModel vm)
                vm.ShowLogoutInfo();

            (Window.GetWindow(this) as Finly.Shell.ShellWindow)?.Close();

            Application.Current.MainWindow = auth;
            auth.Show();
        }

        // ================== LISTA / FILTRY ==================

        private void LoadExpenses()
        {
            var expenses = new List<ExpenseDisplayModel>();

            using (var connection = new SQLiteConnection(DatabaseService.ConnectionString))
            {
                connection.Open();
                SchemaService.Ensure(connection);

                const string query = @"
SELECT e.Id, e.Amount, e.Date, e.Description, c.Name
FROM Expenses e
LEFT JOIN Categories c ON e.CategoryId = c.Id
WHERE e.UserId = @userId;";

                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@userId", _userId);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    expenses.Add(new ExpenseDisplayModel
                    {
                        Id = reader.GetInt32(0),
                        Amount = reader.GetDouble(1),
                        Date = DateTime.Parse(reader.GetString(2)), // ISO yyyy-MM-dd
                        Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Category = reader.IsDBNull(4) ? "Brak kategorii" : reader.GetString(4),
                        UserId = _userId
                    });
                }
            }

            _expenses = expenses;
            ExpenseListView.ItemsSource = _expenses;

            TotalAmountText.Text = _expenses.Sum(e => e.Amount).ToString("0.00") + " zł";
            EntryCountText.Text = _expenses.Count.ToString();

            if (_expenses.Any())
            {
                var days = (_expenses.Max(e => e.Date) - _expenses.Min(e => e.Date)).TotalDays + 1;
                var average = _expenses.Sum(e => e.Amount) / days;
                DailyAverageText.Text = $"{average:0.00} zł";
            }
            else
            {
                DailyAverageText.Text = "0 zł";
            }
        }

        private void LoadCategories()
        {
            var categories = _expenses
                .Select(e => e.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            CategoryFilterComboBox.ItemsSource = categories;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategory = (CategoryFilterComboBox.Text ?? string.Empty).Trim();
            DateTime? from = FromDatePicker.SelectedDate;
            DateTime? to = ToDatePicker.SelectedDate;

            var filtered = _expenses.Where(exp =>
                   (string.IsNullOrWhiteSpace(selectedCategory) || exp.Category == selectedCategory)
                && (!from.HasValue || exp.Date >= from.Value)
                && (!to.HasValue || exp.Date <= to.Value))
                .ToList();

            ExpenseListView.ItemsSource = filtered;
        }

        // ================== AKCJE NA WIERSZU ==================

        private void EditExpense_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var expense = DatabaseService.GetExpenseById(id);
                if (expense == null) return;

                var editView = new EditExpenseView(expense, _userId)
                {
                    Owner = Window.GetWindow(this)
                };
                editView.ShowDialog();

                LoadExpenses();
                LoadCategories();
            }
        }

        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var dlg = new ConfirmDialog("Czy na pewno chcesz usunąć ten wydatek?");
                dlg.Owner = Window.GetWindow(this);

                if (dlg.ShowDialog() == true && dlg.Result)
                {
                    DatabaseService.DeleteExpense(id);
                    ToastService.Success("Usunięto wydatek.");
                    LoadExpenses();
                    LoadCategories();
                }
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (ExpenseListView.SelectedItem is ExpenseDisplayModel item)
            {
                var dlg = new Finly.Views.ConfirmDialog("Czy na pewno chcesz usunąć ten wydatek?");
                dlg.Owner = Window.GetWindow(this);
                if (dlg.ShowDialog() == true && dlg.Result)
                {
                    DatabaseService.DeleteExpense(item.Id); // <-- tu był błąd (id -> item.Id)
                    ToastService.Success("Usunięto wydatek.");
                    LoadExpenses();
                    LoadCategories();
                }
            }
        }


        // (opcjonalnie) double-click ↔ edycja
        private void ExpenseListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExpenseListView.SelectedItem is ExpenseDisplayModel selectedExpense)
            {
                var fullExpense = DatabaseService.GetExpenseById(selectedExpense.Id);
                if (fullExpense is null) return;

                var editView = new EditExpenseView(fullExpense, _userId)
                {
                    Owner = Window.GetWindow(this)
                };
                editView.ShowDialog();

                LoadExpenses();
                LoadCategories();
            }
        }
    }
}
