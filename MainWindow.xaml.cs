using System.Windows;
using System.Windows.Input;

namespace Finly.Views
{
    public partial class MainWindow : Window
    {
        // stan dla F11 (tryb bez ramek)
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private WindowState _prevState;

        public MainWindow()
        {
            InitializeComponent();

            // twarde ustawienie (gdyby styl globalny został nadpisany gdzieś lokalnie)
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResize;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // „dobicie” po załadowaniu – pewność, że jest fullscreen i ma ☐
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResize;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
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
