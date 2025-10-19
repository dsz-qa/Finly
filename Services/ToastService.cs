// Finly/Services/ToastService.cs
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Finly.Shell; // ShellWindow, ToastControl

namespace Finly.Services
{
    /// <summary>
    /// Prosty serwis do wyświetlania toastów (Info / Success / Warning / Error).
    /// Wymaga w ShellWindow elementu:  <StackPanel x:Name="ToastLayer" .../>
    /// </summary>
    public static class ToastService
    {
        /// <summary>
        /// Główny „push” – dodaje ToastControl do warstwy w ShellWindow.
        /// </summary>
        private static void Push(string message, string type)
        {
            // Aplikacja może nie mieć okna (unit testy / start)
            var app = Application.Current;
            if (app is null) return;

            void DoWork()
            {
                // Znajdź aktualny ShellWindow
                var shell = app.Windows.OfType<ShellWindow>().FirstOrDefault();
                if (shell is null) return;

                // Znajdź warstwę na toasty
                if (shell.FindName("ToastLayer") is not Panel layer) return;

                // Dodaj nowy toast
                var toast = new ToastControl(message ?? string.Empty, type ?? "info");
                layer.Children.Add(toast);
            }

            // Zapewnij wykonanie na wątku UI
            if (app.Dispatcher.CheckAccess())
                DoWork();
            else
                app.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(_ =>
                {
                    DoWork();
                    return null;
                }), null);
        }
        public enum ToastPosition { BottomCenter, TopRight }

        public static ToastPosition Position { get; private set; } = ToastPosition.BottomCenter;

        public static void SetPosition(ToastPosition position)
        {
            Position = position;
        }

        public static void Show(string message, string type = "info")
        {
            var shell = Application.Current.Windows.OfType<Finly.Shell.ShellWindow>().FirstOrDefault();
            if (shell == null) return;
            if (shell.FindName("ToastLayer") is not Canvas canvas) return;

            var toast = new Finly.Shell.ToastControl(message, type);
            canvas.Children.Add(toast);

            toast.Loaded += (_, __) =>
            {
                if (Position == ToastPosition.BottomCenter)
                {
                    // --- dół-środek (stackowanie w górę) ---
                    const double bottom = 24;
                    const double spacing = 10;

                    double y = bottom;
                    foreach (FrameworkElement child in canvas.Children.OfType<FrameworkElement>())
                    {
                        // ustaw od dołu; środek w poziomie
                        child.UpdateLayout();
                    }

                    // po załadowaniu policz ułożenie
                    double curBottom = bottom;
                    foreach (FrameworkElement child in canvas.Children.OfType<FrameworkElement>())
                    {
                        Canvas.SetLeft(child, (canvas.ActualWidth - child.ActualWidth) / 2);
                        Canvas.SetTop(child, canvas.ActualHeight - curBottom - child.ActualHeight);
                        curBottom += child.ActualHeight + spacing;
                    }
                }
                else
                {
                    // --- prawy-górny róg (stackowanie w dół) ---
                    const double top = 20, right = 20, spacing = 10;

                    double y = top;
                    foreach (FrameworkElement child in canvas.Children.OfType<FrameworkElement>())
                    {
                        child.UpdateLayout();
                        Canvas.SetTop(child, y);
                        Canvas.SetRight(child, right);
                        y += child.ActualHeight + spacing;
                    }
                }
            };
        }

        // Wygodne skróty:
        public static void Info(string message) => Push(message, "info");
        public static void Success(string message) => Push(message, "success");
        public static void Warning(string message) => Push(message, "warning");
        public static void Error(string message) => Push(message, "error");

        /// <summary>
        /// Czyści wszystkie aktualnie wyświetlane toasty (opcjonalnie).
        /// </summary>
        public static void Clear()
        {
            var app = Application.Current;
            if (app is null) return;

            void DoWork()
            {
                var shell = app.Windows.OfType<ShellWindow>().FirstOrDefault();
                if (shell?.FindName("ToastLayer") is Panel layer)
                    layer.Children.Clear();
            }

            if (app.Dispatcher.CheckAccess())
                DoWork();
            else
                app.Dispatcher.BeginInvoke((System.Action)DoWork);
        }
    }
}
