using System;
using System.Windows;
using Aplikacja_do_sledzenia_wydatkow.Models;
using Aplikacja_do_sledzenia_wydatkow.Models;
using Aplikacja_do_sledzenia_wydatkow.Services;
using Aplikacja_do_sledzenia_wydatkow.Services;

namespace Aplikacja_do_sledzenia_wydatkow.Views
{
    public partial class EditExpenseView : Window
    {
        private readonly int _userId;
        private readonly Expense _existingExpense;

        public EditExpenseView(Expense expense, int userId)
        {
            InitializeComponent();
            _userId = userId;
            _existingExpense = expense;

            // Wype³nij pola formularza
            AmountBox.Text = expense.Amount.ToString();
            CategoryBox.Text = DatabaseService.GetCategoryNameById(expense.CategoryId); // zak³adamy, ¿e taka metoda istnieje
            DateBox.SelectedDate = expense.Date;
            DescriptionBox.Text = expense.Description;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(AmountBox.Text, out double amount) &&
                !string.IsNullOrWhiteSpace(CategoryBox.Text) &&
                DateBox.SelectedDate.HasValue)
            {
                string categoryName = CategoryBox.Text.Trim();
                int categoryId = DatabaseService.GetOrCreateCategoryId(categoryName, _userId);

                _existingExpense.Amount = amount;
                _existingExpense.CategoryId = categoryId;
                _existingExpense.Date = DateBox.SelectedDate.Value;
                _existingExpense.Description = DescriptionBox.Text;

                DatabaseService.UpdateExpense(_existingExpense);
                MessageBox.Show("Zapisano zmiany.");
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Uzupe³nij wszystkie pola.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}