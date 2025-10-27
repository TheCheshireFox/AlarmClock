using AlarmClock.Audio.AudioSink;
using AlarmClock.Shared;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Audio.AudioDevice;

internal class AudioSession : IAudioSession
{
    private static readonly IAudioSink _nullSink = new NullAudioSink();

    private readonly IAudioSource _source;
    private readonly IAudioSink _sink;
    private readonly IAudioSessionHost _audioSessionHost;
    private readonly ILogger _logger;
    private readonly byte[] _buffer;

    private readonly BackgroundTaskService _pumpBackgroundService;
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private IAudioSink _currentSink;
    
    public string Name => _source.GetType().Name;
    public bool Finished => _finished.Task.IsCompleted;
    public AudioFormat Format { get; }

    public AudioSession(IAudioSource source, IAudioSink sink, IAudioSessionHost audioSessionHost, ILogger logger)
    {
        _source = source;
        _sink = sink;
        _audioSessionHost = audioSessionHost;
        _logger = logger;
        _currentSink = sink;
        _buffer = new byte[sink.Format.FrameSize];
        Format = sink.Format;

        _pumpBackgroundService = new BackgroundTaskService(PumpAsync);
    }

    public async Task WaitAsync(CancellationToken cancellationToken) => await _finished.Task.WaitAsync(cancellationToken);

    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        if (!_pumpBackgroundService.IsRunning)
            return;

        if (!_source.CanPause)
        {
            SwitchSink(_nullSink);
            return;
        }

        await _source.PauseAsync(cancellationToken);
        await _pumpBackgroundService.StopAsync(cancellationToken);
        await _sink.FlushAsync(cancellationToken);
    }

    public async Task ResumeAsync(CancellationToken cancellationToken)
    {
        if (_pumpBackgroundService.IsRunning)
        {
            if (!_source.CanPause)
                SwitchSink(_sink);
            
            return;
        }
        
        await _source.StartAsync(cancellationToken);
        await _pumpBackgroundService.StartAsync(cancellationToken);
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        var buffer = new Memory<byte>(_buffer);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await _source.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;

                await _currentSink.WriteAsync(buffer[..read], cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
#pragma warning disable CA2254
            _logger.LogError(ex, cancellationToken.IsCancellationRequested ? "Session pump cancelled" : "Session pump failed");
#pragma warning restore CA2254
        }
        
        await _sink.FlushAsync(cancellationToken).WithExceptionLogging(_logger);
        await _audioSessionHost.NotifySessionFinishAsync(this).WithExceptionLogging(_logger);
        _finished.TrySetResult();
    }


    private void SwitchSink(IAudioSink sink)
    {
        Interlocked.Exchange(ref _currentSink, sink);
    }

    public async ValueTask DisposeAsync()
    {
        await _pumpBackgroundService.StopAsync(CancellationToken.None);
        await _audioSessionHost.NotifySessionFinishAsync(this);
        
        _pumpBackgroundService.Dispose();
        
        _finished.TrySetResult();
    }
}