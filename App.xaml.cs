using System.Windows;
using Finly.Views;

namespace Finly
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Jedno główne okno (brak StartupUri w App.xaml, żeby nie było dwóch okien)
            var auth = new AuthWindow();
            MainWindow = auth;
            auth.Show();
        }
    }
}
