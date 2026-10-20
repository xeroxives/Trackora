using System;

namespace WpfApp14.Interfaces
{
    public interface I_UiThread
    {
        void Invoke(Action action);
    }
}