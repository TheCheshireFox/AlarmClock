using System.Diagnostics;
using AlarmClock.Process;

namespace AlarmClock.Audio.AudioSink;

public sealed class SoxAudioSink : IAudioSink, IAsyncDisposable
{
    private ScopedProcess? _soxProcess;
    private readonly string[] _args;
    private readonly SemaphoreSlim _lock = new(1);
    private readonly CancellationTokenSource _cts = new();

    public AudioFormat Format { get; } = new(2, 48000, 16, AudioEncoding.Signed, true, 2048);
    
    public SoxAudioSink()
    {
        _args = FormatArgs(Format).ToArray();
        _soxProcess = CreateSoxProcess();
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureStartedAsync();
            await _soxProcess!.StandardInput.WriteAsync(buffer, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureStartedAsync()
    {
        if (_soxProcess?.Process.HasExited is false)
            return;
        
        if (_soxProcess != null)
            await _soxProcess.DisposeAsync();

        _soxProcess = CreateSoxProcess();
    }

    private ScopedProcess CreateSoxProcess() => new (new ProcessStartInfo("sox", _args)
    {
        RedirectStandardInput = true
    }, cancellationToken: _cts.Token);

    private static IEnumerable<string> FormatArgs(AudioFormat format)
    {
        // basic parameters
        yield return "-q";
        yield return "-V1";
        yield return "--ignore-length";
        yield return "--buffer"; yield return format.FrameSize.ToString();
        
        yield return "-t"; yield return "raw";
        yield return "-r"; yield return format.SampleRate.ToString();
        yield return "-e"; yield return format.Encoding switch
        {
            AudioEncoding.Signed => "signed-integer",
            AudioEncoding.Unsigned => "unsigned-integer",
            AudioEncoding.FloatingPoint => "floating-point",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        yield return "-b"; yield return format.BitsPerSample.ToString();
        yield return "-c"; yield return format.Channels.ToString();
        yield return format.LittleEndian ? "-L" : "-B";
        yield return "-";
        
        yield return "-d";
    }
    
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        
        if (_soxProcess != null)
            await _soxProcess.DisposeAsync();
    }
}