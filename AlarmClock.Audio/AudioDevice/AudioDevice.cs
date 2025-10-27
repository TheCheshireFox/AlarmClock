using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Shared;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Audio.AudioDevice;

internal class AudioPriorityComparer : IComparer<AudioPriority>
{
    public int Compare(AudioPriority x, AudioPriority y) => (int)y - (int)x;
}

internal interface IAudioSessionHost
{
    Task NotifySessionFinishAsync(AudioSession session);
}

public sealed class AudioDevice : IAudioDevice, IAudioSessionHost, IAsyncDisposable
{
    private readonly IAudioSink _sink;
    private readonly ILogger<AudioDevice> _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly SemaphoreSlim _queueLock = new(1);
    private readonly PriorityQueue<AudioSession, AudioPriority> _queue = new(new AudioPriorityComparer());
    private readonly Channel<AudioSession> _sessionsToFinish;
    private readonly BackgroundTaskService _sessionsFinisherService;

    private SessionInfo? _current;

    public AudioDevice(IAudioSink sink, ILogger<AudioDevice> logger, ILoggerFactory loggerFactory)
    {
        _sink = sink;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _sessionsToFinish = Channel.CreateUnbounded<AudioSession>();
        
        _sessionsFinisherService = new BackgroundTaskService(SessionsFinisherAsync);
        _sessionsFinisherService.StartAsync(CancellationToken.None).Wait();
    }

    public async Task<IAudioSession> OpenSessionAsync(IAudioSource source, AudioPriority priority, CancellationToken cancellationToken)
    {
        using (await _queueLock.LockAsync(cancellationToken))
        {
            if (_current is not { } current)
                return await SetCurrentAsync(source, priority, cancellationToken);

            if (priority == AudioPriority.Exclusive && current.Priority == AudioPriority.Exclusive)
                throw new InvalidOperationException("Another exclusive audio session in progress");

            if (current.Priority < priority)
            {
                await ReturnToQueue(current, cancellationToken);
                return await SetCurrentAsync(source, priority, cancellationToken);
            }

            var session = new AudioSession(source, _sink, this, CreateAudioSessionLogger(source));
            await source.InitializeAsync(_sink.Format, cancellationToken);
            _queue.Enqueue(session, priority);

            _logger.LogInformation("Session enqueued: {Name}", session.Name);

            return session;
        }
    }

    Task IAudioSessionHost.NotifySessionFinishAsync(AudioSession session)
    {
        _ = _sessionsToFinish.Writer.TryWrite(session);
        return Task.CompletedTask;
    }

    private async Task SessionsFinisherAsync(CancellationToken cancellationToken)
    {
        await foreach (var session in _sessionsToFinish.Reader.ReadAllAsync(cancellationToken))
        {
            await FinishSessionInternalAsync(session, cancellationToken);
        }
    }

    private async Task FinishSessionInternalAsync(AudioSession session, CancellationToken cancellationToken)
    {
        using (await _queueLock.LockAsync(cancellationToken))
        {
            if (_current == null)
                return;

            if (_current.Session != session)
            {
                _logger.LogWarning("Session finished while another session is active");
                return;
            }

            _logger.LogInformation("Session finished: {Name}", session.Name);

            while (_queue.TryDequeue(out var newSession, out var priority))
            {
                if (newSession.Finished)
                    continue;

                _current = new SessionInfo(newSession, priority);
                await _current.Session.ResumeAsync(cancellationToken);

                _logger.LogInformation("Session resumed: {Name}", newSession.Name);

                return;
            }

            _current = null;
        }
    }

    private async Task ReturnToQueue(SessionInfo session, CancellationToken cancellationToken)
    {
        await session.Session.PauseAsync(cancellationToken);

        _queue.Enqueue(session.Session, session.Priority);

        _logger.LogInformation("Session returned to queue: {Name}", session.Session.Name);
    }

    private async Task<AudioSession> SetCurrentAsync(IAudioSource source, AudioPriority priority, CancellationToken cancellationToken)
    {
        if (_current is { } current)
        {
            _logger.LogInformation("Set active session: {Name}, priority: {Priority}, from: {PrevName}, priority: {PrevPriority}",
                source.GetType().Name, priority, current.Session.Name, current.Priority);
        }
        else
        {
            _logger.LogInformation("Set active session: {Name}, priority: {Priority}", source.GetType().Name, priority);
        }

        _current = new SessionInfo(new AudioSession(source, _sink, this, CreateAudioSessionLogger(source)), priority);
        await source.InitializeAsync(_sink.Format, cancellationToken);
        await _current.Session.ResumeAsync(cancellationToken);

        return _current.Session;
    }

    private ILogger CreateAudioSessionLogger(IAudioSource source) => _loggerFactory.CreateLogger($"{nameof(AudioSession)}.{source.GetType().Name}");
    
    private record SessionInfo(AudioSession Session, AudioPriority Priority);

    public async ValueTask DisposeAsync()
    {
        _sessionsToFinish.Writer.Complete();
        await _sessionsFinisherService.StopAsync(CancellationToken.None);
        
        _sessionsFinisherService.Dispose();
        _queueLock.Dispose();
    }
}