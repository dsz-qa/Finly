using System.Windows;
using Finly.Views; // jeśli AuthWindow jest w Finly.Views

namespace Finly
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var auth = new Finly.Views.AuthWindow();
            Current.MainWindow = auth;
            auth.Show();
        }

    }
}


