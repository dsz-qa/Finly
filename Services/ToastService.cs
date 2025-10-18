using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Finly.Shell;

namespace Finly.Services
{
    public static class ToastService
    {
        public static void Show(string message, string type = "info")
        {
            var shell = Application.Current.Windows.OfType<ShellWindow>().FirstOrDefault();
            if (shell == null) return;

            if (shell.FindName("ToastLayer") is not Canvas canvas) return;

            var toast = new ToastControl(message, type);
            canvas.Children.Add(toast);

            // ustaw pozycję w prawym górnym rogu, jedna pod drugą
            const double top = 20, right = 20, spacing = 10;
            double y = top;
            foreach (var child in canvas.Children.OfType<FrameworkElement>())
            {
                Canvas.SetTop(child, y);
                y += child.ActualHeight + spacing;
            }
            Canvas.SetRight(toast, right);

            toast.Loaded += (_, __) =>
            {
                Canvas.SetTop(toast, y - (toast.ActualHeight + spacing));
                Canvas.SetRight(toast, right);
            };
        }
    }
}
