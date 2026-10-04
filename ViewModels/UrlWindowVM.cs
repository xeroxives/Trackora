using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp14.Views;

namespace WpfApp14.ViewModels
{
    public class UrlWindowVM : VM
    {
        #region Private
        private readonly string _saveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Trackora","Songs"
        );
        #endregion

        #region Getters
        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; Notify(nameof(DialogResult)); }
        }

        private string _url;
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                Notify(nameof(Url));
            }
        }

        private bool _isHighQuality;
        public bool IsHighQuality
        {
            get => _isHighQuality;
            set
            {
                _isHighQuality = value;
                Notify(nameof(IsHighQuality));
            }
        }
        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; Notify(nameof(IsDownloading)); }
        }
        #endregion

        #region Command
        public ICommand GoCommand { get; }
        #endregion

        #region Private_Funcs
        private void Go()
        {
            if (string.IsNullOrWhiteSpace(_url)) return;

            if (!Directory.Exists(_saveFolder))
            {
                try { Directory.CreateDirectory(_saveFolder); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось создать папку: {ex.Message}");
                    return;
                }
            }

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
            if (!File.Exists(exePath))
                exePath = Path.Combine(_saveFolder, "yt-dlp.exe");

            if (!File.Exists(exePath))
            {
                MessageBox.Show("Файл yt-dlp.exe не найден!");
                return;
            }

            IsDownloading = true;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = BuildArgs(_url, IsHighQuality),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new Process { StartInfo = startInfo };
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        IsDownloading = false;

                        if (Application.Current.Windows.OfType<UrlWindow>().FirstOrDefault() is UrlWindow win
                            && win.IsLoaded)
                        {
                            bool success = process.ExitCode == 0;

                            if (!success)
                            {
                                MessageBox.Show(
                                    $"Ошибка скачивания (код {process.ExitCode}).",
                                    "Ошибка",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }

                            win.DialogResult = success;
                        }
                        else
                        {
                            MessageBox.Show("Скачивание завершено, но окно ещё не готово к закрытию.");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };

                process.Start();
            }
            catch (Exception ex)
            {
                IsDownloading = false;
                MessageBox.Show($"Ошибка запуска: {ex.Message}");
            }
        }

        private string BuildArgs(string url, bool highQuality)
        {
            string qualityArg = highQuality ? "--audio-quality 256K " : "";
            return $"-x --audio-format mp3 {qualityArg}--retries 3 --socket-timeout 5 -o \"{_saveFolder}\\%(title)s.%(ext)s\" \"{url}\"";
        }
        #endregion

        #region Constructor
        public UrlWindowVM()
        {
            GoCommand = new ButtonCommand(Go);
        }
        #endregion
    }
}