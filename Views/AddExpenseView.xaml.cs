using System;
using System.Windows;
using Aplikacja_do_sledzenia_wydatkow.Models;
using Aplikacja_do_sledzenia_wydatkow.Services;

namespace Aplikacja_do_sledzenia_wydatkow.Views
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
                string.IsNullOrWhiteSpace(categoryComboBox.Text) ||
                !datePicker.SelectedDate.HasValue ||
                string.IsNullOrWhiteSpace(descriptionBox.Text))
            {
                MessageBox.Show("Uzupe³nij wszystkie pola.");
                return;
            }

            if (!double.TryParse(AmountBox.Text, out double amount))
            {
                MessageBox.Show("WprowadŸ poprawn¹ kwotê.");
                return;
            }

            string categoryName = categoryComboBox.Text.Trim();
            int categoryId = DatabaseService.GetOrCreateCategoryId(categoryName, _userId);

            var expense = new Expense
            {
                Amount = amount,
                CategoryId = categoryId,
                Date = datePicker.SelectedDate.Value,
                Description = descriptionBox.Text,
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