using System;
using System.Windows;
using System.Windows.Controls;
using Finly.Services;

namespace Finly.Pages
{
    public partial class AddExpensePage : UserControl
    {
        private readonly int _userId;

        //  Ten konstruktor jest wywoływany przez ShellWindow
        public AddExpensePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        //  Dodatkowy konstruktor bez parametrów (jeśli ktoś używa go w XAML)
        public AddExpensePage() : this(UserService.CurrentUserId) { }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CategoryBox.Text) ||
                    string.IsNullOrWhiteSpace(AmountBox.Text))
                {
                    ToastService.Warning("Uzupełnij kategorię i kwotę.");
                    return;
                }

                if (!decimal.TryParse(AmountBox.Text, out decimal amount))
                {
                    ToastService.Error("Wprowadź poprawną kwotę (np. 123.45).");
                    return;
                }

                if (_userId <= 0)
                {
                    ToastService.Error("Brak zalogowanego użytkownika.");
                    return;
                }

                //  Dodaj wydatek (tworzy kategorię, jeśli jej nie ma)
                ExpenseService.AddExpense(
                    userId: _userId,
                    categoryName: CategoryBox.Text.Trim(),
                    amount: amount,
                    description: DescriptionBox.Text,
                    date: DateBox.SelectedDate ?? DateTime.Now
                );

                ToastService.Success("Wydatek został dodany pomyślnie!");
                CategoryService.GetCategorySummary(_userId); // aktualizuje dane w tle

                CategoryBox.Text = string.Empty;
                AmountBox.Text = string.Empty;
                DescriptionBox.Text = string.Empty;
                DateBox.SelectedDate = DateTime.Now;

                (Window.GetWindow(this) as Finly.Shell.ShellWindow)?.NavigateTo("Dashboard");
            }
            catch (Exception ex)
            {
                ToastService.Error($"Błąd podczas dodawania wydatku: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            (Window.GetWindow(this) as Finly.Shell.ShellWindow)?.NavigateTo("Dashboard");
        }
    }
}