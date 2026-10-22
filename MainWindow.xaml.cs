using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Trackora.Core.Interfaces;
using WpfApp14.Services;
using WpfApp14.ViewModels;
using Trackora.Core.ViewModels;
using SharpVectors.Runtime;
using SharpVectors.Converters;
using WpfApp14.Utils;
using Trackora.Core.Utils;

namespace WpfApp14
{
    public partial class MainWindow : Window
    {
        private Slider _progressSlider;
        private bool _isDragging = false;

        // Экземпляры сервисов, которыми владеет окно
        private readonly I_AudioService _audioService;

        public MainWindow()
        {
            // ===== Composition Root: собираем все зависимости =====
            _audioService = new AudioService();
            var uiTimer = new WpfUiTimer();
            var uiThread = new WpfUiThread();
            var pathProvider = new WpfPathProvider();
            var filePicker = new WpfFilePicker();
            var osActions = new WpfOsActions();
            var dialogService = new WpfDialogService();
            var fileHasher = new WpfFileHasher();
            var dbService = new DbServiceWrapper();

            // Главный VM получает все сервисы через конструктор
            var mainVM = new MainVM(
                audioService: _audioService,
                uiTimer: uiTimer,
                uiThread: uiThread,
                pathProvider: pathProvider,
                filePicker: filePicker,
                osActions: osActions,
                dialogService: dialogService,
                fileHasher: fileHasher,
                dbService: dbService
            );

            DataContext = mainVM;

            StateChanged += MainWindow_StateChanged;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SetButtonSvg(Button button, string svgPath)
        {
            SvgViewbox s = new SvgViewbox();
            s.Source = new Uri(svgPath, UriKind.Relative);
            button.Content = s;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (BtnMaximize != null)
            {
                Debug.WriteLine("Change size");
                string svgPath = WindowState == WindowState.Maximized
                    ? "./Assets/windowed_btn_1.svg"
                    : "./Assets/windowed_btn_2.svg";

                string tooltip = WindowState == WindowState.Maximized
                    ? "Свернуть в окно"
                    : "Развернуть";

                SetButtonSvg(BtnMaximize, svgPath);
                BtnMaximize.ToolTip = tooltip;
            }
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
            if (sender is Slider slider && DataContext is MainVM mainVM
                && mainVM.CurrentVM is I_PlayerVM playerVM)
            {
                Logger.Log($"[SLIDER] Drag completed. Value: {slider.Value:F2}s");
                playerVM.SeekTo(slider.Value);

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

            // Используем интерфейс, а не конкретный тип — универсальнее и чище
            if (sender is Slider slider && DataContext is MainVM mainVM
                && mainVM.CurrentVM is I_PlayerVM playerVM)
            {
                double clickPos = e.GetPosition(slider).X;
                double ratio = clickPos / slider.ActualWidth;
                if (ratio < 0) ratio = 0;
                if (ratio > 1) ratio = 1;

                double seekValue = ratio * slider.Maximum;

                Logger.Log($"[SLIDER] Click seek. Ratio: {ratio:F3}, Value: {seekValue:F2}s");

                playerVM.SeekTo(seekValue);

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
            // 1. Просим VM сохранить статистику и освободить ресурсы
            if (DataContext is MainVM mainVM && mainVM.CurrentVM is I_PlayerVM playerVM)
            {
                playerVM.Cleanup();
            }

            // 2. Освобождаем глобальные ресурсы аудио-сервиса
            _audioService?.Dispose();

            base.OnClosed(e);
        }
    }
}