using System;

namespace Trackora.Core.Interfaces
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