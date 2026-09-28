using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp14.Converters;
using WpfApp14.Models;
using WpfApp14.Services;
using WpfApp14.Utils;

namespace WpfApp14.ViewModels
{
	public class MainVM : VM
	{
        #region Getters
        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();
        public ObservableCollection<Song> Songs
        {
            get => _songs;
            set { _songs = value; Notify(nameof(Songs)); }
        }

        private Song _selectedSong;
        public Song SelectedSong
        {
            get => _selectedSong;
            set
            {
                if (_selectedSong != value && value != null)
                {
                    _selectedSong = value;
                    Notify(nameof(SelectedSong));

                    if (!string.IsNullOrEmpty(value.FilePathOrUri))
                    {
                        _audioService.Play(value.FilePathOrUri);
                        _isPlaying = true;
                        StatusText = $"Playing: {value.Title}";

                        // <s/> Запускаем таймер ПОСЛЕ старта аудио
                        StartProgressTimer();
                    }
                    else
                    {
                        StopProgressTimer();
                    }
                }
            }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; Notify(nameof(StatusText)); }
        }

        // <s/> Новые свойства для прогресс-бара
        private double _currentPositionSeconds = 0;
        public double CurrentPositionSeconds
        {
            get => _currentPositionSeconds;
            set { _currentPositionSeconds = value; Notify(nameof(CurrentPositionSeconds)); }
        }

        private double _totalDurationSeconds = 0;
        public double TotalDurationSeconds
        {
            get => _totalDurationSeconds;
            set { _totalDurationSeconds = value; Notify(nameof(TotalDurationSeconds)); }
        }
        #endregion

        #region Privates
        private AudioService _audioService = new AudioService();
        private bool _isPlaying = false;
        private DispatcherTimer _progressTimer;
        #endregion

        public ICommand LoadTestDataCommand { get; }
		public ICommand PlayPauseCommand { get; }
		public ICommand OpenFileCommand { get; }

        public MainVM()
        {
            LoadTestDataCommand = new ButtonCommand(LoadTestData);
            PlayPauseCommand = new ButtonCommand(TogglePlayPause);
            OpenFileCommand = new ButtonCommand(OpenFileDialog);

            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(30);
            _progressTimer.Tick += ProgressTimer_Tick;

            _audioService.TrackEnded += () =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _isPlaying = false;
                    StatusText = "Finished";
                    StopProgressTimer();
                    Notify(nameof(StatusText));
                    Notify(nameof(SelectedSong));
                });
            };

            _audioService.DurationUpdated += (seconds) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (SelectedSong != null)
                    {
                        SelectedSong.DurationSeconds = seconds;
                        TotalDurationSeconds = seconds;

                        // <s/> Явно уведомляем об изменении конкретных свойств модели
                        Notify(nameof(SelectedSong));
                        Notify(nameof(TotalDurationSeconds));
                    }
                });
            };

            Logger.Log("MainVM initialized with AudioService and Timer.");
        }
        #region Funcs
        private void StartProgressTimer()
        {
            // Обновляем общую длительность перед запуском
            _totalDurationSeconds = _audioService.Duration.TotalSeconds;
            Notify(nameof(TotalDurationSeconds));

            // Сбрасываем позицию в 0 при старте нового трека
            _currentPositionSeconds = 0;
            Notify(nameof(CurrentPositionSeconds));

            if (!_progressTimer.IsEnabled)
                _progressTimer.Start();

            Logger.Log("Progress timer started.");
        }

        private void StopProgressTimer()
        {
            if (_progressTimer.IsEnabled)
                _progressTimer.Stop();
        }

        // <s/> Обработчик тика таймера
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_isPlaying && _audioService.IsPlaying)
            {
                double pos = _audioService.Position.TotalSeconds;

                if (double.IsNaN(pos) || double.IsInfinity(pos)) pos = 0;

                CurrentPositionSeconds = pos;
            }
            else if (!_isPlaying)
            {
                StopProgressTimer();
            }
        }
        #endregion
        private void OpenFileDialog()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav|All Files|*.*",
                Title = "Select Music File"
            };

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;

                string title = Path.GetFileNameWithoutExtension(path);
                string artistName = "Unknown";
                string genre = "N/A";
                double duration = 0; // <s/> Локальная переменная для хранения длины

                try
                {
                    using (var file = TagLib.File.Create(path))
                    {
                        if (!string.IsNullOrEmpty(file.Tag.Title))
                            title = file.Tag.Title;

                        if (file.Tag.Performers.Length > 0)
                            artistName = file.Tag.Performers[0];

                        if (file.Tag.Genres.Length > 0 && !string.IsNullOrEmpty(file.Tag.Genres[0]))
                            genre = file.Tag.Genres[0];

                        // <s/> Получаем длину напрямую из тега, чтобы избежать гонки событий
                        duration = file.Properties.Duration.TotalSeconds;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Tag parse failed for {path}: {ex.Message}. Using filename.");
                    // Если парсер упал, пробуем получить длину через BASS позже, но пока ставим 0
                }

                var newSong = new Song
                {
                    Id = Songs.Count + 1,
                    Title = title,
                    FilePathOrUri = path,
                    Genre = genre,
                    DurationSeconds = duration, // <s/> Сразу заполняем длительность!
                    Info = $"Artist: {artistName}"
                };

                Songs.Add(newSong);

                // Теперь SelectedSong триггерит Play, а Duration уже есть в объекте
                SelectedSong = newSong;
            }
        }

        private void LoadTestData()
		{
			Logger.Log("Loading test data...");
			Songs.Clear();
			Songs.Add(new Song { Id = 1, Title = "Rumbling Hearts", ArtistId = 1, Genre = "Anime", DurationSeconds = 285, BitrateKbps = 320 });
			Songs.Add(new Song { Id = 2, Title = "Harder, Better...", ArtistId = 2, Genre = "House", DurationSeconds = 229, BitrateKbps = 256, Bpm = 123 });
			StatusText = $"Loaded {Songs.Count} tracks.";
			Logger.Log($"Test data loaded successfully. Count: {Songs.Count}");
		}

        private void TogglePlayPause()
        {
            if (SelectedSong == null || string.IsNullOrEmpty(SelectedSong.FilePathOrUri))
            {
                Logger.Error("No valid track selected.");
                return;
            }

            if (_isPlaying)
            {
                _audioService.Pause();
                _isPlaying = false;
                StatusText = "Paused";
                StopProgressTimer(); // Останавливаем обновление
            }
            else
            {
                // Проверяем, нужно ли начинать заново или продолжить
                if (_audioService.Position > TimeSpan.Zero && _audioService.Duration > TimeSpan.Zero)
                    _audioService.Resume();
                else
                    _audioService.Play(SelectedSong.FilePathOrUri);

                _isPlaying = true;
                StatusText = $"Playing: {SelectedSong.Title}";
                StartProgressTimer(); // Запускаем обновление
            }
            Notify(nameof(StatusText));
        }
        public void Cleanup()
		{
			_audioService?.Dispose();
		}
	}
}