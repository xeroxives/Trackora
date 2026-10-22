using Trackora.Core.Interfaces;

namespace WpfApp14.Services
{
    public class WpfFilePicker : I_FilePicker
    {
        public string PickAudioFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.wma;*.aac|All Files|*.*",
                Title = "Select Music File"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}