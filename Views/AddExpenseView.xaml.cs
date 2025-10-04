using System;
using System.Windows;
using Finly.Models;
using Finly.Services;

namespace Finly.Views
{
    public partial class AddExpenseView : Window
    {
        private readonly int _userId;

        public AddExpenseView(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja pól
            if (string.IsNullOrWhiteSpace(AmountBox.Text) ||
                string.IsNullOrWhiteSpace(CategoryBox.Text) ||
                !DateBox.SelectedDate.HasValue ||
                string.IsNullOrWhiteSpace(DescriptionBox.Text))
            {
                MessageBox.Show("Uzupe³nij wszystkie pola.");
                return;
            }

            if (!double.TryParse(AmountBox.Text, out double amount))
            {
                MessageBox.Show("WprowadŸ poprawn¹ kwotê.");
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
            MessageBox.Show("Wydatek zosta³ dodany.");
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
