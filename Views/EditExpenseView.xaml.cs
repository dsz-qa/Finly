using System;
using System.Windows;
using Finly.Models;
using Finly.Services;

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

            // Pre-fill formularza
            AmountBox.Text = _existingExpense.Amount.ToString("0.##");
            CategoryBox.Text = DatabaseService.GetCategoryNameById(_existingExpense.CategoryId) ?? string.Empty;
            DateBox.SelectedDate = _existingExpense.Date;
            DescriptionBox.Text = _existingExpense.Description ?? string.Empty;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja (bez MessageBox – u¿ywamy ³adnych toastów)
            if (!double.TryParse(AmountBox.Text, out var amount))
            {
                ToastService.Warning("Wpisz poprawn¹ kwotê.");
                return;
            }

            if (string.IsNullOrWhiteSpace(CategoryBox.Text))
            {
                ToastService.Warning("Podaj kategoriê.");
                return;
            }

            if (!DateBox.SelectedDate.HasValue)
            {
                ToastService.Warning("Wybierz datê.");
                return;
            }

            // Aktualizacja modelu i zapis
            string categoryName = CategoryBox.Text.Trim();
            int categoryId = DatabaseService.GetOrCreateCategoryId(categoryName, _userId);

            _existingExpense.Amount = amount;
            _existingExpense.CategoryId = categoryId;
            _existingExpense.Date = DateBox.SelectedDate.Value;
            _existingExpense.Description = DescriptionBox.Text ?? string.Empty;

            DatabaseService.UpdateExpense(_existingExpense);

            // Info dla u¿ytkownika + zamkniêcie okna
            ToastService.Success("Zapisano zmiany.");
            DialogResult = true;   // pomocne dla okna wywo³uj¹cego (jeœli u¿ywa ShowDialog)
            Close();
        }

        // (opcjonalnie – jeœli masz przycisk Anuluj, pod³¹cz w XAML do tej metody)
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
