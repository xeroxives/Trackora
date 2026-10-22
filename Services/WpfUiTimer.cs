using System;
using System.Windows.Threading;
using Trackora.Core.Interfaces;

namespace WpfApp14.Services
{
    public class WpfUiTimer : I_UITimer
    {
        private readonly DispatcherTimer _timer = new();

        public WpfUiTimer()
        {
            _timer.Tick += (s, e) => Tick?.Invoke(s, e);
        }

        public event EventHandler Tick;

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public bool IsEnabled => _timer.IsEnabled;
        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
        
    }
}