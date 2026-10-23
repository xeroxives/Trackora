using System;
using Microsoft.Maui.ApplicationModel;
using Trackora.Core.Interfaces;

namespace Trackora.Android.Services;

public class MauiUiThread : I_UiThread
{
    public void Invoke(Action action)
    {
        if (MainThread.IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }
}
