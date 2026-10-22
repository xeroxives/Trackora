using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trackora.Core.Models;

namespace Trackora.Core.Interfaces
{
    public interface I_DialogService
    {
        void ShowEditMetadataDialog(Song song);
        void ShowUrlDialog();
    }
}
