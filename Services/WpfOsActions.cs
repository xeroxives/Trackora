using System.Diagnostics;
using WpfApp14.Interfaces;

namespace WpfApp14.Services
{
    public class WpfOsActions : I_OsActions
    {
        public void ShowInExplorer(string filePath)
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }
}