using System.Diagnostics;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Process;
using AlarmClock.Shared.Extensions;

namespace AlarmClock.Audio.AudioSource;

public sealed class SoxWavAudioSource : IAudioSource, IAsyncDisposable
{
    private readonly Stream _wavStream;
    private readonly string[] _effectsArgs;
    private readonly CancellationTokenSource _cts = new();
    
    private bool _paused;
    
    private ScopedProcess? _process;
    private Task _copyTask = Task.CompletedTask;

    public SoxWavAudioSource(Stream wavStream, string[] effectsArgs)
    {
        _wavStream = wavStream;
        _effectsArgs = effectsArgs;
    }

    public Task InitializeAsync(AudioFormat format, CancellationToken cancellationToken)
    {
        if (_process != null)
            return Task.CompletedTask;
        
        _process = new ScopedProcess(new ProcessStartInfo("sox", CreateArguments(format, _effectsArgs))
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        }, cancellationToken: _cts.Token);
        
        _copyTask = _wavStream.CopyToAsync(_process.StandardInput, _cts.Token);
        
        return Task.CompletedTask;
    }

    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_process == null)
            throw new Exception("Source not initialized");
        
        if (_paused)
            throw new InvalidOperationException($"{nameof(SoxWavAudioSource)} is paused");
        
        _copyTask.ThrowIfFailed();
        return await _process.StandardOutput.ReadAsync(buffer, cancellationToken);
    }

    public Task PauseAsync(CancellationToken cancellationToken)
    {
        _paused = true;
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _paused = false;
        return Task.CompletedTask;
    }

    private static IEnumerable<string> CreateArguments(AudioFormat format, string[] effectsArgs)
    {
        yield return "--buffer"; yield return format.FrameSize.ToString();
        yield return "-t"; yield return "wav"; yield return "-";
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

        foreach (var arg in effectsArgs)
            yield return arg;
    }
    
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        if (_process != null) await _process.DisposeAsync();
        await _copyTask.WithExceptionLogging();
        await _wavStream.DisposeAsync();
    }
}