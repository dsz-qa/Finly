using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Finly.Shell
{
    public partial class ToastControl : UserControl
    {
        public ToastControl(string message, string type = "info")
        {
            InitializeComponent();
            Msg.Text = message;

            // prosta kolorystyka wg typu
            var color = type switch
            {
                "success" => "#2e7d32",
                "error" => "#c62828",
                "warn" => "#f9a825",
                _ => "#333333"
            };
            Root.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color)!;

            Loaded += (_, __) => AnimateInThenOut();
        }

        private void AnimateInThenOut()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            var stay = new DoubleAnimation(1, 1, TimeSpan.FromSeconds(2)) { BeginTime = TimeSpan.FromMilliseconds(150) };
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200)) { BeginTime = TimeSpan.FromSeconds(2.15) };
            fadeOut.Completed += (_, __) =>
            {
                if (Parent is Panel p) p.Children.Remove(this);
            };

            this.BeginAnimation(OpacityProperty, fadeIn);
            this.BeginAnimation(OpacityProperty, stay);
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
