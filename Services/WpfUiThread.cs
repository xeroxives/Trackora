using System;
using System.Windows;
using WpfApp14.Interfaces;

namespace WpfApp14.Services
{
    public class WpfUiThread : I_UiThread
    {
        public void Invoke(Action action)
        {
            if (Application.Current == null || Application.Current.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
                action();
            else
                Application.Current.Dispatcher.Invoke(action);
            
        }
    }
}