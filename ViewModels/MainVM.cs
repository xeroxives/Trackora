using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    Logger.Log($"[TRACK] Switching from '{_selectedSong?.Title}' to '{value.Title}'. Resetting position.");

                    _selectedSong = value;
                    Notify(nameof(SelectedSong));

                    _lastKnownPosition = 0;
                    _currentPositionSeconds = 0;
                    Notify(nameof(CurrentPositionSeconds));

                    if (!string.IsNullOrEmpty(value.FilePathOrUri))
                    {
                        _audioService.Play(value.FilePathOrUri);
                        _isPlaying = true;
                        StatusText = $"Playing: {value.Title}";
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

        private double _currentPositionSeconds = 0;
        public double CurrentPositionSeconds
        {
            get => _currentPositionSeconds;
            set
            {
                _currentPositionSeconds = value;
                Notify(nameof(CurrentPositionSeconds));

                if (_isUserSeeking)
                {
                    _lastKnownPosition = value;       // Обновляем кэш при seek
                    _audioService.Seek(value);
                    _isUserSeeking = false;
                }
            }
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
        private readonly string _appDataFolder;
        private bool _isSeeking = false;
        private bool _isUserSeeking = false;
        private double _lastKnownPosition = 0;
        private int _tickCounter = 0;
        #endregion

        #region I_Command
        public ICommand LoadTestDataCommand { get; }
        public ICommand PlayPauseCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand SaveToAppDataCommand { get; }
        public ICommand RemoveFromSessionCommand { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand EditMetadataCommand { get; }
        public ICommand AddToQueueCommand { get; }
        #endregion

        #region Funcs_Timer
        private void StartProgressTimer()
        {
            _totalDurationSeconds = _audioService.Duration.TotalSeconds;
            Notify(nameof(TotalDurationSeconds));

            _currentPositionSeconds = _lastKnownPosition;
            Notify(nameof(CurrentPositionSeconds));

            _tickCounter = 0;

            if (!_progressTimer.IsEnabled)
                _progressTimer.Start();

            Logger.Log($"[TIMER] Started. Pos: {_lastKnownPosition:F2}s, Duration: {_totalDurationSeconds:F2}s, Timer enabled: {_progressTimer.IsEnabled}");
        }

        private void StopProgressTimer()
        {
            Logger.Log($"[TIMER] Stopped. Was enabled: {_progressTimer.IsEnabled}, LastPos: {_lastKnownPosition:F2}s");

            if (_progressTimer.IsEnabled)
                _progressTimer.Stop();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            _tickCounter++;

            if (_isPlaying && _audioService.IsPlaying)
            {
                if (_isUserSeeking)
                {
                    Logger.Log($"[TICK #{_tickCounter}] SKIPPED — user is seeking.");
                    return;
                }

                double pos = _audioService.Position.TotalSeconds;

                if (_tickCounter % 30 == 0)
                {
                    Logger.Log($"[TICK #{_tickCounter}] BASS pos: {pos:F2}s, LastKnown: {_lastKnownPosition:F2}s, CurrentUI: {_currentPositionSeconds:F2}s");
                }

                if (!double.IsNaN(pos) && !double.IsInfinity(pos) && pos >= 0)
                {
                    _lastKnownPosition = pos;
                    _currentPositionSeconds = pos;
                    Notify(nameof(CurrentPositionSeconds));
                }
            }
            else if (!_isPlaying)
            {
                Logger.Log($"[TICK #{_tickCounter}] STOPPED — _isPlaying is false.");
                StopProgressTimer();
            }
            else
            {
                Logger.Log($"[TICK #{_tickCounter}] SKIPPED — _isPlaying={_isPlaying}, BASS.IsPlaying={_audioService.IsPlaying}");
            }
        }
        #endregion

        #region Public
        public void SeekTo(double seconds)
        {
            Logger.Log($"[SEEK] Begin. Target: {seconds:F2}s, Timer running: {_progressTimer.IsEnabled}, IsPlaying: {_isPlaying}, BASS.IsPlaying: {_audioService.IsPlaying}");

            _isUserSeeking = true;
            _lastKnownPosition = seconds;
            _audioService.Seek(seconds);
            _currentPositionSeconds = seconds;
            Notify(nameof(CurrentPositionSeconds));
            _isUserSeeking = false;

            Logger.Log($"[SEEK] End. CurrentPos: {_currentPositionSeconds:F2}s, LastKnown: {_lastKnownPosition:F2}s");
        }
        public void Cleanup()
        {
            _audioService?.Dispose();
        }
        #endregion

        #region Context Menu Logic
        private void SaveToAppData(object param)
        {
            if (!(param is Song song)) return;

            try
            {
                if (song.IsSavedToAppData)
                {
                    // === УДАЛЕНИЕ из AppData ===

                    // Удаляем аудиофайл
                    if (!string.IsNullOrEmpty(song.AppDataFilePath) && File.Exists(song.AppDataFilePath))
                    {
                        File.Delete(song.AppDataFilePath);
                        Logger.Log($"Deleted from AppData: {song.AppDataFilePath}");
                    }

                    // Удаляем обложку, если она хранится в AppData
                    if (!string.IsNullOrEmpty(song.BackgroundImagePath) &&
                        song.BackgroundImagePath.StartsWith(_appDataFolder) &&
                        File.Exists(song.BackgroundImagePath))
                    {
                        File.Delete(song.BackgroundImagePath);
                        Logger.Log($"Deleted cover from AppData: {song.BackgroundImagePath}");
                    }

                    // Если трек сейчас играет — останавливаем, т.к. файл удалён
                    if (SelectedSong == song)
                    {
                        _audioService.Stop();
                        _isPlaying = false;
                        StopProgressTimer();
                        CurrentPositionSeconds = 0;
                        TotalDurationSeconds = 0;
                        StatusText = "Playback stopped — file removed from library.";
                    }

                    song.IsSavedToAppData = false;
                    song.AppDataFilePath = null;

                    Logger.Log($"Removed '{song.Title}' from library.");
                    StatusText = $"Removed '{song.Title}' from library.";
                }
                else
                {
                    // === СОХРАНЕНИЕ в AppData ===

                    // Проверяем, что исходный файл существует
                    if (string.IsNullOrEmpty(song.FilePathOrUri) || !File.Exists(song.FilePathOrUri))
                    {
                        Logger.Error($"Cannot save to AppData — source file not found: {song.FilePathOrUri}");
                        StatusText = "Error: Source file not found.";
                        return;
                    }

                    string fileName = Path.GetFileName(song.FilePathOrUri);
                    string destPath = Path.Combine(_appDataFolder, fileName);

                    // Избегаем дубликатов имён файлов
                    int counter = 1;
                    while (File.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(_appDataFolder, $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }

                    // Копируем аудиофайл в AppData
                    File.Copy(song.FilePathOrUri, destPath, true);
                    Logger.Log($"Copied to AppData: {destPath}");

                    // Извлекаем обложку из оригинального файла и сохраняем рядом
                    try
                    {
                        using (var tagFile = TagLib.File.Create(song.FilePathOrUri))
                        {
                            if (tagFile.Tag.Pictures != null && tagFile.Tag.Pictures.Length > 0)
                            {
                                var pic = tagFile.Tag.Pictures[0];

                                string coversDir = Path.Combine(_appDataFolder, "covers");
                                if (!Directory.Exists(coversDir))
                                    Directory.CreateDirectory(coversDir);

                                // Определяем расширение обложки по MIME-типу
                                string coverExt = ".jpg";
                                if (!string.IsNullOrEmpty(pic.MimeType))
                                {
                                    if (pic.MimeType.Contains("png")) coverExt = ".png";
                                    else if (pic.MimeType.Contains("gif")) coverExt = ".gif";
                                    else if (pic.MimeType.Contains("bmp")) coverExt = ".bmp";
                                }

                                string coverFileName = $"cover_{song.Id}{coverExt}";
                                string coverFullPath = Path.Combine(coversDir, coverFileName);

                                File.WriteAllBytes(coverFullPath, pic.Data.Data);
                                song.BackgroundImagePath = coverFullPath;

                                Logger.Log($"Cover saved: {coverFullPath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Cover extraction failed for '{song.Title}': {ex.Message}");
                        // Обложка не критична — продолжаем без неё
                    }

                    // Обновляем путь воспроизведения на копию в AppData
                    // Теперь трек будет играться из AppData, а не из оригинального расположения
                    song.FilePathOrUri = destPath;
                    song.AppDataFilePath = destPath;
                    song.IsSavedToAppData = true;

                    Logger.Log($"Saved '{song.Title}' to library: {destPath}");
                    StatusText = $"Saved '{song.Title}' to Trackora library.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied during AppData operation: {ex.Message}");
                StatusText = "Error: Access denied. Check file permissions.";
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error during AppData operation: {ex.Message}");
                StatusText = "Error: Could not copy/delete file. It may be in use.";
            }
            catch (Exception ex)
            {
                Logger.Error($"AppData operation failed: {ex.Message}");
                StatusText = "Error: Could not save/remove file.";
            }
        }

        private void RemoveFromSession(object param)
        {
            if (!(param is Song song)) return;

            // Если удаляемый трек сейчас играет - останавливаем
            if (SelectedSong == song)
            {
                _audioService.Stop();
                _isPlaying = false;
                StopProgressTimer();
                SelectedSong = null;
            }

            Songs.Remove(song);
            StatusText = $"Removed '{song.Title}' from session.";
            Logger.Log($"Removed from session: {song.Title}");
        }

        private void OpenFileLocation(object param)
        {
            if (!(param is Song song)) return;

            string pathToOpen = song.IsSavedToAppData && !string.IsNullOrEmpty(song.AppDataFilePath)
                ? song.AppDataFilePath
                : song.FilePathOrUri;

            if (string.IsNullOrEmpty(pathToOpen) || !File.Exists(pathToOpen))
            {
                StatusText = "File location not found.";
                return;
            }

            try
            {
                // Открываем проводник с выделенным файлом
                Process.Start("explorer.exe", $"/select,\"{pathToOpen}\"");
                Logger.Log($"Opened location: {pathToOpen}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open location: {ex.Message}");
                StatusText = "Could not open file location.";
            }
        }

        private void EditMetadata(object param)
        {
            if (!(param is Song song)) return;

            // ЗАГЛУШКА: Редактирование метаданных требует отдельного окна
            StatusText = $"Edit mode for '{song.Title}' (Not implemented yet)";
            Logger.Log($"Edit requested for: {song.Title}");

            // TODO: Здесь будет открытие EditSongWindow(song)
        }

        private void AddToQueue(object param)
        {
            if (!(param is Song song)) return;
            StatusText = $"Added '{song.Title}' to queue (Stub)";
            Logger.Log($"Add to queue requested: {song.Title}");
        }

        #endregion

        #region Funsc_Private
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
                double duration = 0;

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
                        duration = file.Properties.Duration.TotalSeconds;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Tag parse failed for {path}: {ex.Message}. Using filename.");
                }

                var newSong = new Song
                {
                    Id = Songs.Count + 1,
                    Title = title,
                    FilePathOrUri = path,
                    Genre = genre,
                    DurationSeconds = duration,
                    Info = $"Artist: {artistName}",
                    IsSavedToAppData = false
                };

                Songs.Add(newSong);
                _lastKnownPosition = 0;
                SelectedSong = newSong;
            }
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
                double posBeforePause = _audioService.Position.TotalSeconds;
                if (!double.IsNaN(posBeforePause) && !double.IsInfinity(posBeforePause) && posBeforePause > 0)
                {
                    _lastKnownPosition = posBeforePause;
                }

                _audioService.Pause();
                _isPlaying = false;
                StatusText = "Paused";
                StopProgressTimer();

                Logger.Log($"[PLAYPAUSE] Paused at: {_lastKnownPosition:F2}s");
            }
            else
            {
                Logger.Log($"[PLAYPAUSE] Resuming. LastKnown: {_lastKnownPosition:F2}s, StreamHandle active: {_audioService.IsPlaying}");

                bool resumed = _audioService.TryResume(_lastKnownPosition);

                if (!resumed)
                {
                    _lastKnownPosition = 0;
                    _audioService.Play(SelectedSong.FilePathOrUri);
                    Logger.Log($"[PLAYPAUSE] Fresh play started.");
                }

                _isPlaying = true;
                StatusText = $"Playing: {SelectedSong.Title}";
                StartProgressTimer();
            }
            Notify(nameof(StatusText));
        }
        private void LoadFromAppData()
        {
            try
            {
                string[] audioExtensions = { ".mp3", ".wav", ".flac", ".ogg", ".wma", ".aac" };
                var files = Directory.GetFiles(_appDataFolder);

                int idCounter = Songs.Count + 1;
                _lastKnownPosition = 0;
                foreach (string filePath in files)
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (!audioExtensions.Contains(ext)) continue;

                    string title = Path.GetFileNameWithoutExtension(filePath);
                    string artistName = "Unknown";
                    string genre = "N/A";
                    double duration = 0;
                    string album = "";
                    string coverPath = "";

                    try
                    {
                        using (var file = TagLib.File.Create(filePath))
                        {
                            if (!string.IsNullOrEmpty(file.Tag.Title))
                                title = file.Tag.Title;

                            if (file.Tag.Performers.Length > 0)
                                artistName = file.Tag.Performers[0];

                            if (file.Tag.Genres.Length > 0 && !string.IsNullOrEmpty(file.Tag.Genres[0]))
                                genre = file.Tag.Genres[0];

                            if (!string.IsNullOrEmpty(file.Tag.Album))
                                album = file.Tag.Album;

                            duration = file.Properties.Duration.TotalSeconds;

                            // Извлекаем обложку, если есть
                            if (file.Tag.Pictures.Length > 0)
                            {
                                var pic = file.Tag.Pictures[0];
                                string coverFileName = $"cover_{idCounter}.jpg";
                                string coverFullPath = Path.Combine(_appDataFolder, "covers", coverFileName);

                                string coversDir = Path.Combine(_appDataFolder, "covers");
                                if (!Directory.Exists(coversDir))
                                    Directory.CreateDirectory(coversDir);

                                System.IO.File.WriteAllBytes(coverFullPath, pic.Data.Data);
                                coverPath = coverFullPath;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Tag parse failed for AppData file {filePath}: {ex.Message}");
                    }

                    var song = new Song
                    {
                        Id = idCounter++,
                        Title = title,
                        FilePathOrUri = filePath,
                        AppDataFilePath = filePath,
                        IsSavedToAppData = true,
                        Genre = genre,
                        Album = album,
                        DurationSeconds = duration,
                        BitrateKbps = 0,
                        Info = $"Artist: {artistName}",
                        BackgroundImagePath = coverPath
                    };

                    Songs.Add(song);
                    Logger.Log($"Loaded from AppData: {title} ({filePath})");
                }

                if (Songs.Count > 0)
                    StatusText = $"Loaded {Songs.Count} track(s) from library.";
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load from AppData: {ex.Message}");
            }
        }
        #endregion

        #region Test
        private void LoadTestData()
        {
            Logger.Log("Loading test data...");
            //Songs.Clear();
            Songs.Add(new Song { Id = 9991, Title = "Rumbling Hearts", ArtistId = 1, Genre = "Anime", DurationSeconds = 285, BitrateKbps = 320, IsSavedToAppData = false });
            Songs.Add(new Song { Id = 9992, Title = "Harder, Better...", ArtistId = 2, Genre = "House", DurationSeconds = 229, BitrateKbps = 256, Bpm = 123, IsSavedToAppData = false });
            StatusText = $"Loaded {Songs.Count} tracks.";
            Logger.Log($"Test data loaded successfully. Count: {Songs.Count}");
        }
        #endregion

        #region Constructor
        public MainVM()
        {
            // Инициализация путей AppData
            _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackora");
            if (!Directory.Exists(_appDataFolder))
                Directory.CreateDirectory(_appDataFolder);

            // Существующие команды
            LoadTestDataCommand = new ButtonCommand(LoadTestData);
            PlayPauseCommand = new ButtonCommand(TogglePlayPause);
            OpenFileCommand = new ButtonCommand(OpenFileDialog);

            // Новые команды (передаем Song как параметр)
            SaveToAppDataCommand = new ButtonCommand(SaveToAppData);
            RemoveFromSessionCommand = new ButtonCommand(RemoveFromSession);
            OpenFileLocationCommand = new ButtonCommand(OpenFileLocation);
            EditMetadataCommand = new ButtonCommand(EditMetadata);
            AddToQueueCommand = new ButtonCommand(AddToQueue);

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
                        Notify(nameof(SelectedSong));
                        Notify(nameof(TotalDurationSeconds));
                    }
                });
            };
            LoadFromAppData();
            Logger.Log("MainVM initialized with AudioService and Timer.");
        }
        #endregion
    }
}