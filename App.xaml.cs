using System.Windows;
using Finly.Views;

namespace Finly
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var auth = new AuthWindow();
            this.MainWindow = auth;   // jedno główne okno
            auth.Show();
        }
    }
}
