using System.Diagnostics;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Process;

namespace AlarmClock.Audio.AudioSource;

public class Mpg123RadioAudioSource(Uri uri) : IAudioSource, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private string _format = string.Empty;
    private int _sampleRate;
    private ScopedProcess? _mpg123Process;

    public Task InitializeAsync(AudioFormat format, CancellationToken cancellationToken)
    {
        _format = format.Encoding switch
        {
            AudioEncoding.Signed => "s",
            AudioEncoding.Unsigned => "u",
            AudioEncoding.FloatingPoint => "f",
            _ => throw new ArgumentOutOfRangeException(nameof(format.Encoding), format.Encoding, "Invalid encoding")
        };
        _format += format.BitsPerSample.ToString();
        _sampleRate = format.SampleRate;

        StartMpg123Process();

        return Task.CompletedTask;
    }

    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_mpg123Process == null)
            throw new InvalidOperationException($"{nameof(Mpg123RadioAudioSource)} is paused");

        return await _mpg123Process.StandardOutput.ReadAsync(buffer, cancellationToken);
    }

    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        if (_mpg123Process == null)
            return;

        await _mpg123Process.TerminateAsync(5000, KillSignal.SIGTERM, cancellationToken);
        await _mpg123Process.DisposeAsync();
        _mpg123Process = null;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_mpg123Process is { Process.HasExited: false })
            return Task.CompletedTask;

        StartMpg123Process();

        return Task.CompletedTask;
    }

    private void StartMpg123Process()
    {
        _mpg123Process = new ScopedProcess(new ProcessStartInfo("mpg123")
        {
            ArgumentList = { "-q", "-s", "-r", _sampleRate.ToString(), "-e", _format, uri.AbsoluteUri },
            RedirectStandardOutput = true
        }, cancellationToken: _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();

        if (_mpg123Process != null)
        {
            await _mpg123Process.DisposeAsync();
            _mpg123Process = null;
        }
    }
}