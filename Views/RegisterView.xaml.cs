using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class RegisterView : Window
    {
        private readonly RegisterViewModel _viewModel;

        // stan dla trybu fullscreen–bez ramek
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private WindowState _prevState;

        public RegisterView()
        {
            InitializeComponent();

            _viewModel = new RegisterViewModel();
            DataContext = _viewModel;
        }

        // PasswordBox nie wspiera klasycznego bindingu – uzupe³niamy VM rêcznie
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                _viewModel.Password = pb.Password ?? string.Empty;
        }

        // NOWE: powtórzenie has³a -> VM.ConfirmPassword
        private void RepeatPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                _viewModel.ConfirmPassword = pb.Password ?? string.Empty;
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
