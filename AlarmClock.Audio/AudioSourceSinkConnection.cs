using AlarmClock.Audio.AudioSink;
using AlarmClock.Audio.AudioSource;
using AlarmClock.Shared;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Audio;

internal sealed class AudioSourceSinkConnection : IAsyncDisposable
{
    private readonly IAudioSink _sink;
    private readonly ILogger _logger;
    private readonly Func<IAudioSource, CancellationToken, Task> _onPlaybackCompleted;
    private readonly byte[] _buffer;
    private readonly BackgroundTaskService _pumpBackgroundService;
    private readonly CancellationTokenSource _cts = new();

    private IAudioSource? _source;

    public AudioSourceSinkConnection(IAudioSink sink, ILogger logger, Func<IAudioSource, CancellationToken, Task> onPlaybackCompleted)
    {
        _logger = logger;
        _onPlaybackCompleted = onPlaybackCompleted;
        _sink = sink;
        _logger = logger;
        _buffer = new byte[sink.Format.FrameSize];
        _pumpBackgroundService = new BackgroundTaskService(PumpAsync);
    }

    public async Task SwitchSourceAsync(IAudioSource? newSource, CancellationToken cancellationToken)
    {
        await _pumpBackgroundService.StopAsync(cancellationToken);
        
        if (_source != null)
            await _source.PauseAsync(cancellationToken);
        
        _source = newSource;

        if (_source != null)
        {
            await _source.InitializeAsync(_sink.Format, cancellationToken);
            await _source.StartAsync(cancellationToken);
            await _pumpBackgroundService.StartAsync(cancellationToken);
        }
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        var buffer = new Memory<byte>(_buffer);
        var eof = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await _source!.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    eof = true;
                    break;
                }

                await _sink.WriteAsync(buffer[..read], cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, cancellationToken.IsCancellationRequested ? "Session pump cancelled" : "Session pump failed");
        }

        var source = _source;
        _source = null;
        
        var cancel = eof ? _cts.Token : cancellationToken;
        
        await _sink.FlushAsync(cancel).WithExceptionLogging(_logger);
        _ = Task.Run(async () =>
        {
            try
            {
                await _onPlaybackCompleted(source!, cancel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playback completed callback failed");
            }
        }, cancel);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _pumpBackgroundService.StopAsync(CancellationToken.None).WithExceptionLogging(_logger);
        
        _pumpBackgroundService.Dispose();
        _cts.Dispose();
    }
}