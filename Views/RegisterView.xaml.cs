using Aplikacja_do_sledzenia_wydatkow.ViewModels;
using System.Windows;

namespace Aplikacja_do_sledzenia_wydatkow.Views
{
    public partial class RegisterView : Window
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterView()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel();
            this.DataContext = _viewModel;

            // najwa¿niejsze: przypisanie has³a PRZED wykonaniem komendy
            RegisterButton.Click += (s, e) =>
            {
                _viewModel.Password = PasswordBox.Password;
                System.Diagnostics.Debug.WriteLine($"[REGISTER] Wpisane has³o: {_viewModel.Password}");
            };
        }
    }
}
