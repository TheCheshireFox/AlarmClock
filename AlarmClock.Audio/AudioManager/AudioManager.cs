using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Audio.AudioSource;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Audio.AudioManager;

public sealed class AudioManager : IAudioManager
{
    private readonly ILogger<AudioManager> _logger;
    private readonly AudioPriorityQueue<AudioSession> _queue = new();
    private readonly AudioSourceSinkConnection _connection;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly SemaphoreSlim _switchLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    
    private AudioSession? _currentSession;
    
    public AudioManager(IAudioSink sink, ILogger<AudioManager> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _connection = new AudioSourceSinkConnection(sink, loggerFactory.CreateLogger<AudioSourceSinkConnection>(), OnPlaybackCompleted);
    }

    public async Task<IAudioSession> OpenSessionAsync(IAudioSource source, AudioPriority priority, CancellationToken cancellationToken)
    {
        var session = new AudioSession(source, TerminateSessionAsync);

        var nextSource = await SwitchCurrentSessionToNewAsync(session, priority, cancellationToken);
        
        if (nextSource != null)
            await SwitchSourceSerializedAsync(nextSource, cancellationToken);
        
        _logger.LogInformation("New audio session created: {Type}", source.GetType().Name);
        return session;
    }

    private async Task TerminateSessionAsync(AudioSession session, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audio session terminating: {Type}", session.Source.GetType().Name);
        await CompleteSessionAsync(session.Source, cancellationToken);
    }
    
    private async Task OnPlaybackCompleted(IAudioSource source, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audio session completing: {Type}", source.GetType().Name);
        await CompleteSessionAsync(source, cancellationToken);
    }

    private async Task CompleteSessionAsync(IAudioSource source, CancellationToken cancellationToken)
    {
        var nextSource = await SwitchCurrentSessionToNextAsync(source, cancellationToken);
        
        if (nextSource != null)
            await SwitchSourceSerializedAsync(nextSource, cancellationToken);
        
        _logger.LogInformation("Audio session completed: {Type}", source.GetType().Name);
    }
    
    private async Task<IAudioSource?> SwitchCurrentSessionToNewAsync(AudioSession session, AudioPriority priority, CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            if (!_queue.TryEnqueue(session, priority))
                throw new InvalidOperationException("Session already exists");

            var activeSession = _queue.GetActive();
            if (activeSession == null)
                throw new InvalidOperationException("Session was added but not in queue");

            if (activeSession != _currentSession)
            {
                _currentSession = activeSession;
                return _currentSession.Source;
            }
        }
        finally
        {
            _stateLock.Release();
        }

        return null;
    }

    private async Task<IAudioSource?> SwitchCurrentSessionToNextAsync(IAudioSource oldSource, CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            var session = _queue.RemoveBy(x => x.Source == oldSource);
            session?.Completed.TrySetResult();

            // no switch if the current session is not ours, e.g., terminating a displaced session 
            if (_currentSession != null && _currentSession.Source != oldSource)
                return null;
            
            _currentSession = _queue.GetActive();
            return _currentSession?.Source;
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    // we can't allow user cancellation at this point yet, otherwise we will have _currentSession and _connection desync
    private async Task SwitchSourceSerializedAsync(IAudioSource? source, CancellationToken _)
    {
        await _switchLock.WaitAsync(_cts.Token);
        try
        {
            await _connection.SwitchSourceAsync(source, _cts.Token);
        }
        finally
        {
            _switchLock.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _connection.DisposeAsync();
        _stateLock.Dispose();
        _switchLock.Dispose();
        _cts.Dispose();
    }
}