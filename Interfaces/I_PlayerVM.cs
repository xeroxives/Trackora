using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp14.Interfaces
{
    public interface I_PlayerVM
    {
        void SeekTo(double seconds);
        void Cleanup();
    }
}