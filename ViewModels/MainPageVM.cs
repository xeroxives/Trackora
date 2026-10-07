using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp14.Models;
using WpfApp14.Services;
using WpfApp14.Utils;
using WpfApp14.Views;
using WpfApp14.Interfaces;

namespace WpfApp14.ViewModels
{
    public class MainPageVM : VM, I_PlayerVM
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

                    if (!string.IsNullOrEmpty(_currentTrackHash) && _currentTrackSeconds > 0)
                    {
                        _listeningStatsService.AddListeningTime(_currentTrackHash, _currentTrackSeconds);
                        Logger.Log($"[STATS] Saved {_currentTrackSeconds:F2}s for hash {_currentTrackHash.Substring(0, 8)}...");
                    }
                    _currentTrackSeconds = 0;

                    _selectedSong = value;
                    Notify(nameof(SelectedSong));

                    _lastKnownPosition = 0;
                    _currentPositionSeconds = 0;
                    _trackEndHandled = false;
                    Notify(nameof(CurrentPositionSeconds));

                    if (!string.IsNullOrEmpty(value.FilePathOrUri))
                    {
                        _currentTrackHash = FileHasher.ComputePartialHash(value.FilePathOrUri);
                        _lastTickTime = DateTime.UtcNow;

                        _audioService.Play(value.FilePathOrUri);
                        _isPlaying = true;
                        StatusText = $"Playing";
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
                    _lastKnownPosition = value;
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
        public PlaybackMode CurrentPlaybackMode
        {
            get => _playbackMode;
            set
            {
                _playbackMode = value;
                Notify(nameof(CurrentPlaybackMode));
                Notify(nameof(IsLoopActive));
                Notify(nameof(IsShuffleActive));

                string modeName = value == PlaybackMode.Loop ? "Loop" : value == PlaybackMode.Shuffle ? "Shuffle" : "Normal";
                StatusText = $"Playback mode: {modeName}";
                Logger.Log($"[MODE] Changed to: {modeName}");
            }
        }

        public bool IsLoopActive => _playbackMode == PlaybackMode.Loop;
        public bool IsShuffleActive => _playbackMode == PlaybackMode.Shuffle;
        #endregion

        #region Privates
        private AudioService _audioService = new AudioService();
        private bool _isPlaying = false;
        private DispatcherTimer _progressTimer;
        private readonly string _appDataFolder;
        private bool _isSeeking = false;
        private bool _isUserSeeking = false;
        private bool _isUserSeekingFlag = false;
        private double _lastKnownPosition = 0;
        private PlaybackMode _playbackMode = PlaybackMode.Normal;
        private Random _random = new Random();
        private int _tickCounter = 0;
        private bool _trackEndHandled = false;
        private DateTime _lastTickTime;
        private ListeningStatsService _listeningStatsService = new ListeningStatsService();
        private string _currentTrackHash;
        private double _currentTrackSeconds;
        #endregion

        #region I_Command
        public ICommand PlayPauseCommand { get; }
        public ICommand PlayPrevCommand { get; }
        public ICommand PlayNextCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand SaveToAppDataCommand { get; }
        public ICommand ReloadSongsCommand { get; }
        public ICommand RemoveFromSessionCommand { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand EditMetadataCommand { get; }
        public ICommand AddToQueueCommand { get; }
        public ICommand ToggleLoopCommand { get; }
        public ICommand ToggleShuffleCommand { get; }
        #endregion

        #region Funcs_Timer
        private void StartProgressTimer()
        {
            _totalDurationSeconds = _audioService.Duration.TotalSeconds;
            Notify(nameof(TotalDurationSeconds));

            _currentPositionSeconds = _lastKnownPosition;
            Notify(nameof(CurrentPositionSeconds));

            _tickCounter = 0;
            _trackEndHandled = false;
            _lastTickTime = DateTime.UtcNow;

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
                if (_isUserSeekingFlag)
                {
                    Logger.Log($"[TICK #{_tickCounter}] SKIPPED — user is seeking.");
                    return;
                }

                double elapsed = (DateTime.UtcNow - _lastTickTime).TotalSeconds;
                _lastTickTime = DateTime.UtcNow;
                _currentTrackSeconds += elapsed;

                double pos = _audioService.Position.TotalSeconds;

                if (_tickCounter % 30 == 0)
                {
                    Logger.Log($"[TICK #{_tickCounter}] BASS pos: {pos:F2}s, LastKnown: {_lastKnownPosition:F2}s, CurrentUI: {_currentPositionSeconds:F2}s, Duration: {_totalDurationSeconds:F2}s, ActiveSeconds: {_currentTrackSeconds:F2}s");
                }

                if (!double.IsNaN(pos) && !double.IsInfinity(pos) && pos >= 0)
                {
                    _lastKnownPosition = pos;
                    _currentPositionSeconds = pos;
                    Notify(nameof(CurrentPositionSeconds));
                }

                if (_totalDurationSeconds > 0 && pos >= _totalDurationSeconds - 0.15 && !_trackEndHandled)
                {
                    _trackEndHandled = true;
                    Logger.Log($"[TICK #{_tickCounter}] Track end detected via timer fallback.");
                    _isPlaying = false;
                    StopProgressTimer();
                    OnTrackEnded();
                }
            }
            else if (!_isPlaying)
            {
                StopProgressTimer();
            }
        }
        #endregion

        #region Public
        public enum PlaybackMode
        {
            Normal,
            Loop,
            Shuffle
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
                    // === Remove From AppData ===

                    if (!string.IsNullOrEmpty(song.AppDataFilePath) && File.Exists(song.AppDataFilePath))
                    {
                        Songs.Remove(song);
                        File.Delete(song.AppDataFilePath);
                        Logger.Log($"Deleted from AppData: {song.AppDataFilePath}");
                    }

                    if (!string.IsNullOrEmpty(song.BackgroundImagePath) &&
                        song.BackgroundImagePath.StartsWith(_appDataFolder) &&
                        File.Exists(song.BackgroundImagePath))
                    {
                        File.Delete(song.BackgroundImagePath);
                        Logger.Log($"Deleted cover from AppData: {song.BackgroundImagePath}");
                    }

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
                    Notify(nameof(Songs));
                }
                else
                {
                    // === Save To AppData ===

                    if (string.IsNullOrEmpty(song.FilePathOrUri) || !File.Exists(song.FilePathOrUri))
                    {
                        Logger.Error($"Cannot save to AppData — source file not found: {song.FilePathOrUri}");
                        StatusText = "Error: Source file not found.";
                        return;
                    }

                    string fileName = Path.GetFileName(song.FilePathOrUri);
                    string destPath = Path.Combine(_appDataFolder, "Songs", fileName);
                    int counter = 1;

                    while (File.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(_appDataFolder,"Songs", $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }
                    File.Copy(song.FilePathOrUri, destPath, true);
                    Logger.Log($"Copied to AppData: {destPath}");

                    try
                    {
                        using (var tagFile = TagLib.File.Create(song.FilePathOrUri))
                        {
                            if (tagFile.Tag.Pictures != null && tagFile.Tag.Pictures.Length > 0)
                            {
                                var pic = tagFile.Tag.Pictures[0];

                                string coversDir = Path.Combine(_appDataFolder, "Covers");
                                if (!Directory.Exists(coversDir))
                                    Directory.CreateDirectory(coversDir);

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
                    }
                    song.FilePathOrUri = destPath;
                    song.AppDataFilePath = destPath;
                    song.IsSavedToAppData = true;

                    Logger.Log($"Saved '{song.Title}' to library: {destPath}");
                    StatusText = $"Saved.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied during AppData operation: {ex.Message}");
                StatusText = "Error: Access denied.";
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
            StatusText = $"Edit mode not implemented yet";
            Logger.Log($"Edit requested for: {song.Title}");
        }
        private void AddToQueue(object param)
        {
            if (!(param is Song song)) return;
            StatusText = $"Added '{song.Title}' to queue";
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
        private void OpenUrlDialogAsync()
        {
            UrlWindow w = new UrlWindow();
            w.ShowDialog();
            ReloadSongs();
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

                if (string.IsNullOrEmpty(_currentTrackHash))
                {
                    _currentTrackHash = FileHasher.ComputePartialHash(SelectedSong.FilePathOrUri);
                }

                _isPlaying = true;
                StatusText = $"Playing";
                StartProgressTimer();
            }
            Notify(nameof(StatusText));
        }
        private void LoadFromAppData()
        {
            try
            {
                string[] audioExtensions = { ".mp3", ".wav", ".flac", ".ogg", ".wma", ".aac" };
                var files = Directory.GetFiles(Path.Combine(_appDataFolder, "Songs"));

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

                            if (file.Tag.Pictures.Length > 0)
                            {
                                var pic = file.Tag.Pictures[0];
                                string coverFileName = $"cover_{idCounter}.jpg";
                                string coverFullPath = Path.Combine(_appDataFolder, "Covers", coverFileName);

                                string coversDir = Path.Combine(_appDataFolder, "Covers");
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
                        Info = $"Artist: {artistName}",
                        BackgroundImagePath = coverPath
                    };

                    Songs.Add(song);
                    Logger.Log($"Loaded from AppData: {title} ({filePath})");
                }

                if (Songs.Count > 0)
                    StatusText = $"Loaded {Songs.Count} track(s)";
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load from AppData: {ex.Message}");
            }
            finally
            {
                Notify(nameof(Songs));
            }
        }
        private void ToggleLoop(object param)
        {
            if (_playbackMode == PlaybackMode.Loop)
                CurrentPlaybackMode = PlaybackMode.Normal;
            else
                CurrentPlaybackMode = PlaybackMode.Loop;
        }
        private void ToggleShuffle(object param)
        {
            if (_playbackMode == PlaybackMode.Shuffle)
                CurrentPlaybackMode = PlaybackMode.Normal;
            else
                CurrentPlaybackMode = PlaybackMode.Shuffle;
        }
        private void OnTrackEnded()
        {
            if (!string.IsNullOrEmpty(_currentTrackHash) && _currentTrackSeconds > 0)
            {
                _listeningStatsService.AddListeningTime(_currentTrackHash, _currentTrackSeconds);
                Logger.Log($"[STATS] Saved {_currentTrackSeconds:F2}s for hash {_currentTrackHash.Substring(0, 8)}...");
            }
            _currentTrackSeconds = 0;

            if (Songs == null || Songs.Count == 0)
            {
                StatusText = "Finished";
                Notify(nameof(StatusText));
                return;
            }

            switch (_playbackMode)
            {
                case PlaybackMode.Loop:
                    PlayNextTrack();
                    break;

                case PlaybackMode.Shuffle:
                    PlayRandomTrack();
                    break;

                default:
                    StatusText = "Finished";
                    Notify(nameof(StatusText));
                    break;
            }
        }
        private void PlayPrevTrack()
        {
            if (!string.IsNullOrEmpty(_currentTrackHash) && _currentTrackSeconds > 0)
            {
                _listeningStatsService.AddListeningTime(_currentTrackHash, _currentTrackSeconds);
            }
            _currentTrackSeconds = 0;

            int currentIndex = Songs.IndexOf(SelectedSong);
            int nextIndex = (currentIndex - 1) % Songs.Count;
            if (nextIndex < 0) nextIndex = Songs.Count - 1;
            Logger.Log($"[LOOP] Playing prev track: {currentIndex} -> {nextIndex}");
            SelectedSong = Songs[nextIndex];

            _currentTrackHash = FileHasher.ComputePartialHash(SelectedSong.FilePathOrUri);
            _lastTickTime = DateTime.UtcNow;
        }
        private void PlayNextTrack()
        {
            if (!string.IsNullOrEmpty(_currentTrackHash) && _currentTrackSeconds > 0)
            {
                _listeningStatsService.AddListeningTime(_currentTrackHash, _currentTrackSeconds);
            }
            _currentTrackSeconds = 0;

            int currentIndex = Songs.IndexOf(SelectedSong);
            int nextIndex = (currentIndex + 1) % Songs.Count;

            Logger.Log($"[LOOP] Playing next track: {currentIndex} -> {nextIndex}");
            SelectedSong = Songs[nextIndex];

            _currentTrackHash = FileHasher.ComputePartialHash(SelectedSong.FilePathOrUri);
            _lastTickTime = DateTime.UtcNow;
        }
        private void PlayRandomTrack()
        {
            if (!string.IsNullOrEmpty(_currentTrackHash) && _currentTrackSeconds > 0)
            {
                _listeningStatsService.AddListeningTime(_currentTrackHash, _currentTrackSeconds);
            }
            _currentTrackSeconds = 0;

            if (Songs.Count <= 1)
            {
                SelectedSong = Songs[0];
                _currentTrackHash = FileHasher.ComputePartialHash(SelectedSong.FilePathOrUri);
                _lastTickTime = DateTime.UtcNow;
                return;
            }

            int currentIndex = Songs.IndexOf(SelectedSong);
            int randomIndex;

            do
            {
                randomIndex = _random.Next(Songs.Count);
            } while (randomIndex == currentIndex);

            Logger.Log($"[SHUFFLE] Playing random track: index {randomIndex}");
            SelectedSong = Songs[randomIndex];

            _currentTrackHash = FileHasher.ComputePartialHash(SelectedSong.FilePathOrUri);
            _lastTickTime = DateTime.UtcNow;
        }
        private void ReloadSongs()
        {
            Songs.Clear();
            LoadFromAppData();
            Notify(nameof(Songs));
        }
        #endregion

        #region Interfaces
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
            if (!string.IsNullOrEmpty(_currentTrackHash) && _currentTrackSeconds > 0)
            {
                _listeningStatsService.AddListeningTime(_currentTrackHash, _currentTrackSeconds);
                Logger.Log($"[STATS] Saved {_currentTrackSeconds:F2}s on cleanup for hash {_currentTrackHash.Substring(0, 8)}...");
            }
            _currentTrackSeconds = 0;

            _audioService?.Dispose();
        }
        #endregion

        #region Constructor
        public MainPageVM()
        {
            _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackora");
            if (!Directory.Exists(_appDataFolder))
                Directory.CreateDirectory(_appDataFolder);

            if (!Directory.Exists(Path.Combine(_appDataFolder, "Songs")))
                Directory.CreateDirectory(Path.Combine(_appDataFolder, "Songs"));

            PlayPauseCommand = new ButtonCommand(TogglePlayPause);
            PlayNextCommand = new ButtonCommand(PlayNextTrack);
            PlayPrevCommand = new ButtonCommand(PlayPrevTrack);


            OpenFileCommand = new ButtonCommand(OpenFileDialog);
            OpenUrlCommand = new ButtonCommand(OpenUrlDialogAsync);
            ToggleLoopCommand = new ButtonCommand(ToggleLoop);
            ToggleShuffleCommand = new ButtonCommand(ToggleShuffle);
            SaveToAppDataCommand = new ButtonCommand(SaveToAppData);
            ReloadSongsCommand = new ButtonCommand(ReloadSongs);
            RemoveFromSessionCommand = new ButtonCommand(RemoveFromSession);
            OpenFileLocationCommand = new ButtonCommand(OpenFileLocation);
            EditMetadataCommand = new ButtonCommand(EditMetadata);
            AddToQueueCommand = new ButtonCommand(AddToQueue);

            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(25);
            _progressTimer.Tick += ProgressTimer_Tick;

            _audioService.TrackEnded += () =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Logger.Log("[BASS] TrackEnded event received in VM.");
                    if (!_trackEndHandled)
                    {
                        _trackEndHandled = true;
                        _isPlaying = false;
                        StopProgressTimer();
                        OnTrackEnded();
                    }
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
