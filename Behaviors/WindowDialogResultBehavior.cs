using System.Windows;

namespace WpfApp14.Behaviors
{
    public static class WindowDialogResultBehavior
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(WindowDialogResultBehavior),
                new PropertyMetadata(null, OnDialogResultChanged));

        public static bool? GetDialogResult(DependencyObject obj) =>
            (bool?)obj.GetValue(DialogResultProperty);

        public static void SetDialogResult(DependencyObject obj, bool? value) =>
            obj.SetValue(DialogResultProperty, value);

        private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && e.NewValue is bool result)
            {
                window.DialogResult = result;
            }
        }
    }
}