using System;

namespace WpfApp14.Interfaces
{
    public interface I_AudioService : IDisposable
    {
        event Action TrackEnded;
        event Action<double> DurationUpdated;

        bool IsPlaying { get; }
        TimeSpan Position { get; }
        TimeSpan Duration { get; }

        void Play(string filePath);
        void Pause();
        bool TryResume(double fromPositionSeconds = 0);
        void Stop();
        void Seek(double seconds);
        void SetVolume(float vol);
    }
}