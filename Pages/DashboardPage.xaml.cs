using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;

using Finly.Models;
using Finly.Services;
using Finly.ViewModels;
// jeśli AuthWindow / EditExpenseView / AddExpenseView są w Finly.Views:
using Finly.Views;

namespace Finly.Pages
{
    public partial class DashboardPage : UserControl
    {
        private readonly int _userId;
        private List<ExpenseDisplayModel> _expenses = new();

        /// <summary>
        /// Konstruktor bezparametrowy – próbuje pobrać bieżącego użytkownika z serwisu.
        /// Jeśli nie masz takiej metody w UserService, użyj drugiego konstruktora (z userId)
        /// albo podmień to na własne źródło.
        /// </summary>
        public DashboardPage()
            : this(GetCurrentUserIdSafe())
        {
        }

        /// <summary>
        /// Konstruktor z userId (możesz go używać kiedy ShellWindow będzie przekazywał id).
        /// </summary>
        public DashboardPage(int userId)
        {
            InitializeComponent();

            _userId = userId;

            // Ładujemy dane po wejściu na stronę
            LoadExpenses();
            LoadCategories();
        }

        // --- Pomocnicze: próba pobrania bieżącego userId z serwisu ---
        private static int GetCurrentUserIdSafe()
        {
            try
            {
                // Jeśli masz taką metodę – super. Jeśli nie, zwróci 0 i dane się nie wczytają, wtedy użyj konstruktora z parametrem.
                return UserService.GetCurrentUserId();
            }
            catch
            {
                return 0;
            }
        }

        // ================== AKCJE Z PRZYCISKÓW DOLNEGO PASKA ==================

        private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            // Bez nowych okien: przełączamy prawy panel w ShellWindow na stronę "AddExpense"
            var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
            shell?.NavigateTo("AddExpense");
        }

        private void ShowChart_Click(object sender, RoutedEventArgs e)
        {
            // Bez nowych okien: przełączenie na stronę "Charts"
            var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
            shell?.NavigateTo("Charts");
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var ask = MessageBox.Show(
                "Na pewno chcesz trwale usunąć konto wraz ze wszystkimi wydatkami i kategoriami?\n" +
                "Tej operacji nie można cofnąć.",
                "Usuń konto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (ask != MessageBoxResult.Yes) return;

            try
            {
                var ok = UserService.DeleteAccount(_userId);
                if (!ok)
                {
                    MessageBox.Show("Nie udało się usunąć konta.", "Błąd",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Powrót do logowania – tu akurat może zostać osobne okno (logowanie/rejestracja)
                var auth = new AuthWindow();
                var vm = (AuthViewModel)auth.DataContext;
                vm.ShowAccountDeletedInfo();

                // Zamknij shella i pokaż logowanie
                var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
                shell?.Close();

                Application.Current.MainWindow = auth;
                auth.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd podczas usuwania konta:\n" + ex.Message,
                                "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Wylogowanie – wracamy do ekranu logowania
            var auth = new AuthWindow();
            var vm = (AuthViewModel)auth.DataContext;
            vm.ShowLogoutInfo();

            var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
            shell?.Close();

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

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", _userId);

                    using (var reader = command.ExecuteReader())
                    {
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

                // TODO: docelowo chcemy to przenieść na stronę "AddExpense" w trybie edycji.
                // Na teraz zostawię stare okno edycji, żebyś mogła normalnie działać:
                var editView = new EditExpenseView(expense, _userId);
                editView.ShowDialog();

                LoadExpenses();
                LoadCategories();
            }
        }

        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = MessageBox.Show("Czy na pewno chcesz usunąć wydatek?",
                                             "Potwierdzenie",
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                DatabaseService.DeleteExpense(id);

                LoadExpenses();
                LoadCategories();
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (ExpenseListView.SelectedItem is ExpenseDisplayModel item)
            {
                var result = MessageBox.Show("Czy na pewno chcesz usunąć wydatek?",
                                             "Potwierdzenie",
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                DatabaseService.DeleteExpense(item.Id);
                LoadExpenses();
                LoadCategories();
            }
        }

        // (opcjonalnie) jeśli nadal masz podpięty double-click na liście
        private void ExpenseListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ExpenseListView.SelectedItem is ExpenseDisplayModel selectedExpense)
            {
                var fullExpense = DatabaseService.GetExpenseById(selectedExpense.Id);
                if (fullExpense is null) return;

                var editView = new EditExpenseView(fullExpense, _userId);
                editView.ShowDialog();
                LoadExpenses();
                LoadCategories();
            }
        }
    }
}
