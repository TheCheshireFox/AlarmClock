using AlarmClock.Audio.AudioSink;

namespace AlarmClock.Audio;

public interface IAudioSource
{
    bool CanPause { get; }
    
    Task InitializeAsync(AudioFormat format, CancellationToken cancellationToken);
    Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    Task PauseAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
}