using System;

namespace Trackora.Core.Interfaces
{
    public interface I_UiThread
    {
        void Invoke(Action action);
    }
}