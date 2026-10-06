using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp14.Interfaces;

namespace WpfApp14.ViewModels
{
    public class SettingsVM:VM,I_PlayerVM
    {
        public SettingsVM()
        {

        }

        public void Cleanup()
        {
            return;
        }

        public void SeekTo(double seconds)
        {
            return;
        }
    }
}
