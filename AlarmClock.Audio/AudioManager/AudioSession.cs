using AlarmClock.Audio.AudioSource;

namespace AlarmClock.Audio.AudioManager;

internal sealed class AudioSession(IAudioSource source, Func<AudioSession, CancellationToken, Task> terminate) : IAudioSession
{
    private Func<AudioSession, CancellationToken, Task>? _terminate = terminate;
    internal IAudioSource Source { get; } = source;
    internal TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync(CancellationToken cancellationToken) => Completed.Task.WaitAsync(cancellationToken);
    public Task TerminateAsync(CancellationToken cancellationToken) => Interlocked.Exchange(ref _terminate, null)?.Invoke(this, cancellationToken) ?? Task.CompletedTask;
    public async ValueTask DisposeAsync() => await TerminateAsync(CancellationToken.None);
}