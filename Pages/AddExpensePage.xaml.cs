using System;
using System.Windows;
using System.Windows.Controls;
using Finly.Models;
using Finly.Services;

namespace Finly.Pages
{
    public partial class AddExpensePage : UserControl
    {
        private readonly int _userId;

        // Jeśli masz UserService.GetCurrentUserId() – super. Inaczej w ShellWindow przekazuj userId do konstruktora.
        public AddExpensePage() : this(SafeGetUserId()) { }

        public AddExpensePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        private static int SafeGetUserId()
        {
            try { return UserService.GetCurrentUserId(); }
            catch { return 0; }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja pól (dopasuj nazwy kontrolek do swoich w XAML)
            if (string.IsNullOrWhiteSpace(AmountBox.Text) ||
                string.IsNullOrWhiteSpace(CategoryBox.Text) ||
                !DateBox.SelectedDate.HasValue ||
                string.IsNullOrWhiteSpace(DescriptionBox.Text))
            {
                MessageBox.Show("Uzupełnij wszystkie pola.");
                return;
            }

            if (!double.TryParse(AmountBox.Text, out double amount))
            {
                MessageBox.Show("Wprowadź poprawną kwotę.");
                return;
            }

            string categoryName = CategoryBox.Text.Trim();
            int categoryId = DatabaseService.GetOrCreateCategoryId(categoryName, _userId);

            var expense = new Expense
            {
                Amount = amount,
                CategoryId = categoryId,
                Date = DateBox.SelectedDate.Value,
                Description = DescriptionBox.Text,
                UserId = _userId
            };

            DatabaseService.AddExpense(expense);
            MessageBox.Show("Wydatek został dodany.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);

            // zamiast zamykać okno – nawiguj do Dashboard
            var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
            shell?.NavigateTo("Dashboard");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // powrót bez zapisu
            var shell = Window.GetWindow(this) as Finly.Shell.ShellWindow;
            shell?.NavigateTo("Dashboard");
        }
    }
}
