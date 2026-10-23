using System;
using System.IO;
using Plugin.Maui.Audio;
using Trackora.Core.Interfaces;
using Trackora.Core.Utils; // Для вашего логгера Logger

namespace Trackora.Android.Services;

public class MauiAudioService : I_AudioService, IDisposable
{
    #region Privates
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _audioPlayer;
    private bool _isPlayingInternal = false;
    private double _cachedDurationSeconds = 0;

    public event Action? TrackEnded;
    public event Action<double>? DurationUpdated;
    #endregion

    #region Getters
    public bool IsPlaying => _isPlayingInternal;

    public TimeSpan Position
    {
        get
        {
            if (_audioPlayer == null) return TimeSpan.Zero;
            return TimeSpan.FromSeconds(_audioPlayer.CurrentPosition);
        }
    }

    public TimeSpan Duration
    {
        get
        {
            if (_audioPlayer == null) return TimeSpan.Zero;
            return TimeSpan.FromSeconds(_audioPlayer.Duration);
        }
    }
    #endregion

    #region Track_Control
    public void Play(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.Error($"File not found: {filePath}");
            return;
        }

        try
        {
            Stop();

            // Открываем файловый поток для чтения трека
            var fileStream = File.OpenRead(filePath);

            // Создаем экземпляр плеера средствами Android / MAUI
            _audioPlayer = _audioManager.CreatePlayer(fileStream);

            if (_audioPlayer == null)
            {
                throw new Exception("MAUI Audio Player failed to create player instance.");
            }

            _cachedDurationSeconds = _audioPlayer.Duration;
            Logger.Log($"Loaded via MAUI Audio. Duration: {_cachedDurationSeconds}s");
            DurationUpdated?.Invoke(_cachedDurationSeconds);

            // Подписываемся на нативное событие окончания воспроизведения трека
            _audioPlayer.PlaybackEnded += OnPlaybackEnded;

            // Устанавливаем громкость (по умолчанию 0.5f, как в вашем WPF коде)
            _audioPlayer.Volume = 0.5f;

            _audioPlayer.Play();
            _isPlayingInternal = true;
            Logger.Log($"Started playback: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Playback init error: {ex.Message}");
            _isPlayingInternal = false;
        }
    }

    public void Pause()
    {
        if (_audioPlayer != null && _isPlayingInternal)
        {
            _audioPlayer.Pause();
            _isPlayingInternal = false;
            Logger.Log("Paused.");
        }
    }

    public bool TryResume(double fromPositionSeconds = 0)
    {
        if (_audioPlayer == null)
        {
            Logger.Log("TryResume: No active player instance.");
            return false;
        }

        try
        {
            if (fromPositionSeconds > 0.1)
            {
                _audioPlayer.Seek(fromPositionSeconds);
                Logger.Log($"TryResume: Explicit seek to {fromPositionSeconds:F2}s before play.");
            }

            _audioPlayer.Play();
            _isPlayingInternal = true;
            Logger.Log("Resumed mobile playback successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"TryResume failed: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        if (_audioPlayer != null)
        {
            _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
            _audioPlayer.Stop();
            _audioPlayer.Dispose();
            _audioPlayer = null;
        }
        _isPlayingInternal = false;
        Logger.Log("Stopped and freed mobile resources.");
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _isPlayingInternal = false;
        Logger.Log("[MAUI Audio] Track ended callback fired.");
        TrackEnded?.Invoke();
    }

    public void Seek(double seconds)
    {
        if (_audioPlayer == null) return;

        _audioPlayer.Seek(seconds);
        Logger.Log($"Seeked to {seconds:F2}s");
    }

    public void SetVolume(double vol) // В зависимости от интерфейса ядра может принимать float или double
    {
        if (_audioPlayer != null)
        {
            double clampedVol = vol;
            if (clampedVol < 0.0) clampedVol = 0.0;
            if (clampedVol > 1.0) clampedVol = 1.0;

            _audioPlayer.Volume = clampedVol;
        }
    }

    // Заглушка, если интерфейс ядра строго требует float в сигнатуре метода
    public void SetVolume(float vol)
    {
        SetVolume((double)vol);
    }
    #endregion

    #region Constructor
    public MauiAudioService()
    {
        // Получаем системный менеджер звука
        _audioManager = AudioManager.Current;
        Logger.Log("MAUI Android audio engine initialized via native API.");
    }
    #endregion

    #region Dispose
    private bool _isDisposed = false;
    public void Dispose()
    {
        if (_isDisposed) return;

        Stop();
        _isDisposed = true;
        Logger.Log("MauiAudioService disposed.");
    }
    #endregion
}
