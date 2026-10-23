using System;
using Microsoft.Maui.Controls;
using Trackora.Core.Interfaces;

namespace Trackora.Android.Services;

public class MauiUiTimer : I_UITimer
{
    private readonly IDispatcherTimer _timer;

    public MauiUiTimer()
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread();

        if (dispatcher == null)
        {
            throw new InvalidOperationException("Не удалось получить Dispatcher для таймера. Возможно, приложение еще не инициализировано.");
        }

        _timer = dispatcher.CreateTimer();
        _timer.Tick += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
    }
    public event EventHandler? Tick;

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public bool IsEnabled => _timer.IsRunning;

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
}
