using AlarmClock.Audio.AudioSink;

namespace AlarmClock.Audio.AudioDevice;

public enum AudioPriority
{
    Low,
    Medium,
    High,
    Exclusive
}

public interface IAudioSession : IAsyncDisposable
{
    AudioFormat Format { get; }
    Task WaitAsync(CancellationToken cancellationToken);
}

public interface IAudioDevice
{
    /// <summary>
    /// Opens an audio session with a given priority.
    /// The session can be queued if a higher or equal priority session is active.
    /// An 'Exclusive' priority session will attempt to pause any active non-exclusive session
    /// or throw an exception if there is an exclusive session in progress
    /// </summary>
    /// <param name="source">The audio source for the session.</param>
    /// <param name="priority">The priority of the audio session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>

    /// <exception cref="InvalidOperationException">Thrown if there is attempt to open exclusive session while another one in progress</exception>
    Task<IAudioSession> OpenSessionAsync(IAudioSource source, AudioPriority priority, CancellationToken cancellationToken);
}