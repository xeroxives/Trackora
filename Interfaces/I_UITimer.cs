using System;

namespace WpfApp14.Interfaces
{
    public interface I_UITimer
    {
        event EventHandler Tick;
        TimeSpan Interval { get; set; }
        bool IsEnabled { get; }
        void Start();
        void Stop();
    }
}