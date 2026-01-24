namespace AlarmClock.Audio.AudioManager;

public interface IAudioSession : IAsyncDisposable
{
    Task WaitAsync(CancellationToken cancellationToken);
    Task TerminateAsync(CancellationToken cancellationToken);
}