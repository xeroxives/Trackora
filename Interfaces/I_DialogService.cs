using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp14.Models;

namespace WpfApp14.Interfaces
{
    public interface I_DialogService
    {
        void ShowEditMetadataDialog(Song song);
        void ShowUrlDialog();
    }
}
