using WpfApp14.Interfaces;
using WpfApp14.Models;
using WpfApp14.Views;

namespace WpfApp14.Services
{
    public class WpfDialogService : I_DialogService
    {
        public void ShowEditMetadataDialog(Song song)
        {
            var w = new EditMetadataWindow(song);
            w.ShowDialog();
        }

        public void ShowUrlDialog()
        {
            var w = new UrlWindow();
            w.ShowDialog();
        }
    }
}