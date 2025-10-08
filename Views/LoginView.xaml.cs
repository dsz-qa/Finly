using System.Windows;
using System.Windows.Input;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private WindowState _prevState;

        public LoginView()
        {
            InitializeComponent();

            _viewModel = new LoginViewModel();
            DataContext = _viewModel;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Username = UsernameBox.Text ?? string.Empty;
            _viewModel.Password = PasswordBox.Password ?? string.Empty;

            if (_viewModel.LoginCommand.CanExecute(null))
                _viewModel.LoginCommand.Execute(null);
        }

        // --- Fullscreen toggle (F11) + Esc zamknij ---
        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) ToggleFullscreen();
            if (e.Key == Key.Escape) Close();
        }

        private void EnterFullscreen()
        {
            _prevStyle = WindowStyle;
            _prevResize = ResizeMode;
            _prevState = WindowState;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }

        private void ExitFullscreen()
        {
            WindowStyle = _prevStyle;
            ResizeMode = _prevResize;
            WindowState = _prevState == WindowState.Minimized ? WindowState.Normal : _prevState;
        }

        private void ToggleFullscreen()
        {
            if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized)
                ExitFullscreen();
            else
                EnterFullscreen();
        }
    }
}
