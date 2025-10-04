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
            MainWindow = auth;
            auth.Show();
        }
    }
}
