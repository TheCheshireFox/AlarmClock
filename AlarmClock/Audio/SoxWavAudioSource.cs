using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Process;
using AlarmClock.Shared.Extensions;

namespace AlarmClock.Audio;

public sealed class SoxWavAudioSource : IAudioSource, IAsyncDisposable
{
    private readonly Stream _wavStream;
    private readonly string[] _effectsArgs;
    private readonly CancellationTokenSource _cts = new();
    
    private ScopedProcess _process = null!;
    private Task _copyTask = Task.CompletedTask;

    public bool CanPause => true;

    public SoxWavAudioSource(Stream wavStream, string[] effectsArgs)
    {
        _wavStream = wavStream;
        _effectsArgs = effectsArgs;
    }

    public Task InitializeAsync(AudioFormat format, CancellationToken cancellationToken)
    {
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
        return await _process.StandardOutput.ReadAsync(buffer, cancellationToken);
    }

    public Task PauseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
        await _process.DisposeAsync();
        await _cts.CancelAsync();
        await _copyTask.WithExceptionLogging();
        await _wavStream.DisposeAsync();
    }
}