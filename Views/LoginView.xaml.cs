using System.Windows;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel; // ustawiamy VM dla bindingów
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // pobierz dane z kontrolek
            _viewModel.Username = UsernameBox.Text ?? string.Empty;
            _viewModel.Password = PasswordBox.Password ?? string.Empty;

            // odpal komendê logowania (ma CanExecute)
            if (_viewModel.LoginCommand.CanExecute(null))
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }
    }
}
