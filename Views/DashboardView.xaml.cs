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

namespace Finly.Views
{
    public partial class DashboardView : Window
    {
        private readonly int _userId;
        private List<ExpenseDisplayModel> _expenses = new();

        public DashboardView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadExpenses();
            LoadCategories();
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var ask = MessageBox.Show(
                "Na pewno chcesz trwale usun¹æ konto wraz ze wszystkimi wydatkami i kategoriami?\n" +
                "Tej operacji nie mo¿na cofn¹æ.",
                "Usuñ konto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (ask != MessageBoxResult.Yes) return;

            try
            {
                var ok = UserService.DeleteAccount(_userId);
                if (!ok)
                {
                    MessageBox.Show("Nie uda³o siê usun¹æ konta.", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Powrót do logowania z banerem „konto usuniête”
                var auth = new AuthWindow();
                var vm = (AuthViewModel)auth.DataContext;
                vm.ShowAccountDeletedInfo();

                Application.Current.MainWindow = auth;
                auth.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wyst¹pi³ b³¹d podczas usuwania konta:\n" + ex.Message,
                                "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowChart_Click(object sender, RoutedEventArgs e)
        {
            var chart = new ChartView(_userId);
            chart.ShowDialog();
        }

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
                                Date = DateTime.Parse(reader.GetString(2)), // w DB tekst ISO yyyy-MM-dd
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

            TotalAmountText.Text = _expenses.Sum(e => e.Amount).ToString("0.00") + " z³";
            EntryCountText.Text = _expenses.Count.ToString();

            if (_expenses.Any())
            {
                var days = (_expenses.Max(e => e.Date) - _expenses.Min(e => e.Date)).TotalDays + 1;
                var average = _expenses.Sum(e => e.Amount) / days;
                DailyAverageText.Text = $"{average:0.00} z³";
            }
            else
            {
                DailyAverageText.Text = "0 z³";
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

        private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            var addView = new AddExpenseView(_userId);
            addView.ShowDialog();
            LoadExpenses();
            LoadCategories();
        }

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

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCategory = CategoryFilterComboBox.Text?.Trim();
            DateTime? from = FromDatePicker.SelectedDate;
            DateTime? to = ToDatePicker.SelectedDate;

            var filtered = _expenses.Where(exp =>
                   (string.IsNullOrWhiteSpace(selectedCategory) || exp.Category == selectedCategory)
                && (!from.HasValue || exp.Date >= from.Value)
                && (!to.HasValue || exp.Date <= to.Value))
                .ToList();

            ExpenseListView.ItemsSource = filtered;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddExpenseView(_userId);
            addWindow.ShowDialog();
            LoadExpenses();
            LoadCategories();
        }

        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (ExpenseListView.SelectedItem is ExpenseDisplayModel expenseToDelete)
            {
                var confirm = MessageBox.Show("Czy na pewno chcesz usun¹æ ten wydatek?",
                                              "Potwierdzenie", MessageBoxButton.YesNo);
                if (confirm == MessageBoxResult.Yes)
                {
                    DatabaseService.DeleteExpense(expenseToDelete.Id);
                    LoadExpenses();
                    LoadCategories();
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano ¿adnego wydatku do usuniêcia.",
                                "B³¹d", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var auth = new AuthWindow();
            var vm = (AuthViewModel)auth.DataContext;
            vm.ShowLogoutInfo();

            Application.Current.MainWindow = auth;
            auth.Show();
            Close();
        }
    }
}

