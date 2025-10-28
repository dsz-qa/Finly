using System;
using System.Windows;
using System.Windows.Controls;

namespace Finly.Helpers
{
    public static class StackPanelSpacing
    {
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.RegisterAttached(
                "Spacing",
                typeof(double),
                typeof(StackPanelSpacing),
                new PropertyMetadata(0d, OnSpacingChanged));

        public static void SetSpacing(DependencyObject element, double value) =>
            element.SetValue(SpacingProperty, value);

        public static double GetSpacing(DependencyObject element) =>
            (double)element.GetValue(SpacingProperty);

        private static readonly DependencyProperty BaseMarginProperty =
            DependencyProperty.RegisterAttached(
                "BaseMargin",
                typeof(Thickness),
                typeof(StackPanelSpacing),
                new PropertyMetadata(default(Thickness)));

        private static Thickness GetBaseMargin(DependencyObject d) =>
            (Thickness)d.GetValue(BaseMarginProperty);

        private static void SetBaseMargin(DependencyObject d, Thickness value) =>
            d.SetValue(BaseMarginProperty, value);

        private static readonly DependencyProperty HasBaseMarginProperty =
            DependencyProperty.RegisterAttached(
                "HasBaseMargin",
                typeof(bool),
                typeof(StackPanelSpacing),
                new PropertyMetadata(false));

        private static bool GetHasBaseMargin(DependencyObject d) =>
            (bool)d.GetValue(HasBaseMarginProperty);

        private static void SetHasBaseMargin(DependencyObject d, bool value) =>
            d.SetValue(HasBaseMarginProperty, value);

        private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not StackPanel sp) return;

            if ((double)e.NewValue > 0)
            {
                sp.Loaded -= Sp_Apply;
                sp.Loaded += Sp_Apply;

                sp.LayoutUpdated -= Sp_Apply;
                sp.LayoutUpdated += Sp_Apply;

                Apply(sp);
            }
            else
            {
                sp.Loaded -= Sp_Apply;
                sp.LayoutUpdated -= Sp_Apply;
                // przy zerowym spacingu przywróć bazowe marginesy
                foreach (UIElement child in sp.Children)
                {
                    if (child is FrameworkElement fe && GetHasBaseMargin(fe))
                        fe.Margin = GetBaseMargin(fe);
                }
            }
        }

        private static void Sp_Apply(object? sender, EventArgs e)
        {
            if (sender is StackPanel sp) Apply(sp);
        }

        private static void Apply(StackPanel sp)
        {
            var spacing = GetSpacing(sp);
            if (spacing <= 0) return;

            for (int i = 0; i < sp.Children.Count; i++)
            {
                if (sp.Children[i] is FrameworkElement fe)
                {
                    if (!GetHasBaseMargin(fe))
                    {
                        SetBaseMargin(fe, fe.Margin);
                        SetHasBaseMargin(fe, true);
                    }

                    var baseM = GetBaseMargin(fe);
                    var m = baseM;

                    if (sp.Orientation == Orientation.Horizontal)
                        m.Left = (i == 0) ? baseM.Left : baseM.Left + spacing;
                    else
                        m.Top = (i == 0) ? baseM.Top : baseM.Top + spacing;

                    fe.Margin = m;
                }
            }
        }
    }
}
