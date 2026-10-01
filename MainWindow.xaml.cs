using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp14.Utils;
using WpfApp14.ViewModels;

namespace WpfApp14
{
    public partial class MainWindow : Window
    {
        private Slider _progressSlider;
        private bool _isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _progressSlider = FindChild<Slider>(this);
            if (_progressSlider != null)
            {
                _progressSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(Slider_DragStarted));
                _progressSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(Slider_DragCompleted));
                _progressSlider.PreviewMouseLeftButtonUp += Slider_PreviewMouseLeftButtonUp;
            }
        }

        private void Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isDragging = true;
            Logger.Log("[SLIDER] Drag started.");
        }

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isDragging = false;
            if (sender is Slider slider && DataContext is MainVM vm)
            {
                Logger.Log($"[SLIDER] Drag completed. Value: {slider.Value:F2}s");
                vm.SeekTo(slider.Value);

                slider.ClearValue(Slider.ValueProperty);
                BindingExpression be = slider.GetBindingExpression(Slider.ValueProperty);
                if (be != null)
                {
                    be.UpdateTarget();
                    Logger.Log("[SLIDER] Binding re-synced after drag.");
                }
            }
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging) return;

            if (sender is Slider slider && DataContext is MainVM vm)
            {
                double clickPos = e.GetPosition(slider).X;
                double ratio = clickPos / slider.ActualWidth;
                if (ratio < 0) ratio = 0;
                if (ratio > 1) ratio = 1;

                double seekValue = ratio * slider.Maximum;

                Logger.Log($"[SLIDER] Click seek. Ratio: {ratio:F3}, Value: {seekValue:F2}s");

                vm.SeekTo(seekValue);

                slider.ClearValue(Slider.ValueProperty);
                BindingExpression be = slider.GetBindingExpression(Slider.ValueProperty);
                if (be != null)
                {
                    be.UpdateTarget();
                    Logger.Log("[SLIDER] Binding re-synced after click.");
                }
            }
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainVM vm)
                vm.Cleanup();
            base.OnClosed(e);
        }
    }
}