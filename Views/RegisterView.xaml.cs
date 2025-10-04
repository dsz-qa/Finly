using System.Windows;
using System.Windows.Controls;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class RegisterView : Window
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterView()
        {
            InitializeComponent();

            _viewModel = new RegisterViewModel();
            DataContext = _viewModel;

            // Has³o z PasswordBox nie wspiera bindowania — aktualizujemy je na bie¿¹co:
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                _viewModel.Password = pb.Password ?? string.Empty;
                // System.Diagnostics.Debug.WriteLine($"[REGISTER] Has³o: {_viewModel.Password}");
            }
        }
    }
}
