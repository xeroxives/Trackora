using System;
using System.IO;
using System.Runtime.InteropServices; // <s/> Для GCHandle
using WpfApp14.Utils;
using Un4seen.Bass; // <s/> Импорт BASS.NET

namespace WpfApp14.Services
{
    public class AudioService : IDisposable
    {
        #region Privates
        private int _streamHandle = 0;
        private bool _isPlayingInternal = false;
        private static AudioService _instance;
        private GCHandle _selfHandle;
        private Un4seen.Bass.SYNCPROC _syncProcDelegate;

        public event Action TrackEnded;
        public event Action<double> DurationUpdated;
        #endregion

        public AudioService()
        {
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                Logger.Error("Failed to initialize BASS audio engine.");
                throw new InvalidOperationException("BASS Init failed");
            }

            _instance = this;
            _syncProcDelegate = OnTrackEndCallback;

            Logger.Log("BASS audio engine initialized.");
        }

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

                _streamHandle = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);

                if (_streamHandle == 0)
                {
                    throw new Exception($"BASS failed to create stream. Error code: {Bass.BASS_ErrorGetCode()}");
                }

                long lenBytes = Bass.BASS_ChannelGetLength(_streamHandle, BASSMode.BASS_POS_BYTE);
                double realDuration = Bass.BASS_ChannelBytes2Seconds(_streamHandle, lenBytes);

                Logger.Log($"Loaded via BASS. Duration: {realDuration}s");
                DurationUpdated?.Invoke(realDuration);

                Bass.BASS_ChannelSetAttribute(_streamHandle, BASSAttribute.BASS_ATTRIB_VOL, 0.5f);

                if (Bass.BASS_ChannelPlay(_streamHandle, false))
                {
                    _isPlayingInternal = true;
                    Logger.Log($"Started playback: {Path.GetFileName(filePath)}");

                    // <s/> Передаем null вместо указателя. Колбэк возьмет данные из _instance
                    Bass.BASS_ChannelSetSync(_streamHandle, BASSSync.BASS_SYNC_END, 0, _syncProcDelegate, IntPtr.Zero);
                }
                else
                {
                    throw new Exception("BASS failed to start channel play.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Playback init error: {ex.Message}");
                _isPlayingInternal = false;
            }
        }

        private static void OnTrackEndCallback(int handle, int channel, int data, IntPtr user)
        {
            try
            {
                // Получаем актуальный экземпляр сервиса через статическое поле
                var service = _instance;

                if (service != null && service._streamHandle == handle)
                {
                    service._isPlayingInternal = false;
                    Logger.Log("Track ended naturally (via Callback).");
                    service.TrackEnded?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in TrackEnd callback: {ex.Message}");
            }

            Bass.BASS_ChannelRemoveSync(handle, data);
        }

        public void Pause()
        {
            if (_streamHandle != 0)
            {
                Bass.BASS_ChannelPause(_streamHandle);
                _isPlayingInternal = false;
                Logger.Log("Paused.");
            }
        }

        public void Resume()
        {
            if (_streamHandle != 0 && !_isPlayingInternal)
            {
                // resume=true означает продолжить с текущей позиции, а не начать сначала
                Bass.BASS_ChannelPlay(_streamHandle, true);
                _isPlayingInternal = true;
                Logger.Log("Resumed.");
            }
        }

        public void Stop()
        {
            if (_streamHandle != 0)
            {
                // Сначала удаляем все синхронизации, чтобы избежать гонок данных
                Bass.BASS_ChannelRemoveSync(_streamHandle, 0); // 0 - удалить все

                Bass.BASS_ChannelStop(_streamHandle);
                Bass.BASS_StreamFree(_streamHandle);
                _streamHandle = 0;
            }

            _isPlayingInternal = false;
            Logger.Log("Stopped and freed resources.");
        }

        public bool IsPlaying => _isPlayingInternal;

        public TimeSpan Position
        {
            get
            {
                if (_streamHandle == 0) return TimeSpan.Zero;
                long posBytes = Bass.BASS_ChannelGetPosition(_streamHandle, BASSMode.BASS_POS_BYTE);
                double posSec = Bass.BASS_ChannelBytes2Seconds(_streamHandle, posBytes);
                return TimeSpan.FromSeconds(posSec);
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (_streamHandle == 0) return TimeSpan.Zero;
                long lenBytes = Bass.BASS_ChannelGetLength(_streamHandle, BASSMode.BASS_POS_BYTE);
                double lenSec = Bass.BASS_ChannelBytes2Seconds(_streamHandle, lenBytes);
                return TimeSpan.FromSeconds(lenSec);
            }
        }

        public void SetVolume(float vol)
        {
            if (_streamHandle != 0)
            {
                // <s/> Замена Math.Clamp на совместимый с .NET FW код
                float clampedVol = vol;
                if (clampedVol < 0f) clampedVol = 0f;
                if (clampedVol > 1f) clampedVol = 1f;

                Bass.BASS_ChannelSetAttribute(_streamHandle, BASSAttribute.BASS_ATTRIB_VOL, clampedVol);
            }
        }

        public void Dispose()
        {
            Stop();
            if (_instance == this)
            {
                _instance = null;
            }

            Bass.BASS_Free();
            Logger.Log("AudioService disposed.");
        }
    }
}