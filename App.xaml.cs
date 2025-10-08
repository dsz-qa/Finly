using System.Windows;
using Finly.Views;

namespace Finly
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var auth = new AuthWindow();

            // TWARDY fullscreen na wypadek, gdyby styl nie zadziałał:
            auth.WindowState = WindowState.Maximized;
            auth.ResizeMode = ResizeMode.CanMinimize;

            MainWindow = auth;
            auth.Show();
        }
    }
}

