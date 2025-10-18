using System;
using System.Windows;
using Finly.Models;
using Finly.Services;   // <- to nam wystarczy (ToastService jest w Finly.Services)

namespace Finly.Views
{
    public partial class EditExpenseView : Window
    {
        private readonly int _userId;
        private readonly Expense _existingExpense;

        public EditExpenseView(Expense expense, int userId)
        {
            InitializeComponent();
            _userId = userId;
            _existingExpense = expense ?? throw new ArgumentNullException(nameof(expense));

            AmountBox.Text = expense.Amount.ToString("0.##");
            CategoryBox.Text = DatabaseService.GetCategoryNameById(expense.CategoryId) ?? string.Empty;
            DateBox.SelectedDate = expense.Date;
            DescriptionBox.Text = expense.Description ?? string.Empty;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(AmountBox.Text, out double amount)) { MessageBox.Show("WprowadŸ poprawn¹ kwotê."); return; }
            if (string.IsNullOrWhiteSpace(CategoryBox.Text)) { MessageBox.Show("Podaj kategoriê."); return; }
            if (!DateBox.SelectedDate.HasValue) { MessageBox.Show("Wybierz datê."); return; }

            string categoryName = CategoryBox.Text.Trim();
            int categoryId = DatabaseService.GetOrCreateCategoryId(categoryName, _userId);

            _existingExpense.Amount = amount;
            _existingExpense.CategoryId = categoryId;
            _existingExpense.Date = DateBox.SelectedDate.Value;
            _existingExpense.Description = DescriptionBox.Text ?? string.Empty;

            DatabaseService.UpdateExpense(_existingExpense);

            // ³adny komunikat
            ToastService.Show("Zapisano zmiany.", "success");

            Close();
        }
    }
}
