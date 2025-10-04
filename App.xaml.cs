using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Aplikacja_do_sledzenia_wydatkow.Views;
using Aplikacja_do_sledzenia_wydatkow.ViewModels;

namespace Aplikacja_do_sledzenia_wydatkow
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var auth = new Views.AuthWindow();
            MainWindow = auth;
            auth.Show();
        }

    }
}
