using AlarmClock.Audio.AudioSink;

namespace AlarmClock.Audio.AudioSource;

public interface IAudioSource
{
    Task InitializeAsync(AudioFormat format, CancellationToken cancellationToken);
    /// <summary/>
    /// <exception cref="InvalidOperationException">if read on paused stream</exception>
    Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    Task PauseAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
}