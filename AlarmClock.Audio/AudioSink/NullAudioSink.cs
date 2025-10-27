namespace AlarmClock.Audio.AudioSink;

public class NullAudioSink : IAudioSink
{
    public AudioFormat Format { get; } = new (0, 0, 0, AudioEncoding.Signed, false);
    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}