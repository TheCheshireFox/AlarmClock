using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioManager;
using AlarmClock.Audio.AudioSource;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Announcer;

public interface IPiperAnnouncerConfig
{
    string Url { get; }
    bool Prefetch { get; }
    TimeSpan Timeout { get; }
}

public sealed class PiperAnnouncer : IAnnouncer, IAsyncDisposable
{
    private static readonly TimeSpan _phraseTimeout = TimeSpan.FromSeconds(5);
    
    private readonly IAudioManager _audioManager;
    private readonly IPiperAnnouncerConfig _config;
    private readonly ILogger<PiperAnnouncer> _logger;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly BlockingCollection<(string, DateTime)> _phrases = new();

    public PiperAnnouncer(IAudioManager audioManager, IPiperAnnouncerConfig config, ILogger<PiperAnnouncer> logger)
    {
        _audioManager = audioManager;
        _config = config;
        _logger = logger;
        _processingTask = Task.Factory.StartNew(ProcessPhrases,  TaskCreationOptions.LongRunning).Unwrap();
    }
    
    public Task EnqueueSayAsync(string text, CancellationToken cancellationToken)
    {
        _phrases.Add((text, DateTime.UtcNow), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ProcessPhrases()
    {
        using var http = new HttpClient();
        
        foreach (var (phrase, requestTime) in _phrases.GetConsumingEnumerable(_cts.Token))
        {
            try
            {
                if (DateTime.UtcNow - requestTime > _phraseTimeout)
                    continue;
                
                await using var resp = await RequestPiperStreamAsync(http, phrase, _cts.Token);
                await using var source = new SoxWavAudioSource(resp.Stream, ["phaser", "0.5", "1.0", "1.5", "0.7", "2", "chorus", "0.7", "0.9", "55", "0.4", "0.25", "2", "-t"]);
                await using var session  = await _audioManager.OpenSessionAsync(source, AudioPriority.High, _cts.Token);

                await session.WaitAsync(_cts.Token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unable to process phrase");
            }
        }
    }

    private async Task<PiperResponseStream> RequestPiperStreamAsync(HttpClient http, string phrase, CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, _config.Url)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { text = phrase }), Encoding.UTF8, "application/json")
        };

        var completionOption = _config.Prefetch
            ? HttpCompletionOption.ResponseContentRead
            : HttpCompletionOption.ResponseHeadersRead;
            
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_config.Timeout);

        var resp = await http.SendAsync(req, completionOption, cts.Token);
        resp.EnsureSuccessStatusCode();
        
        return new PiperResponseStream(resp, await resp.Content.ReadAsStreamAsync(cts.Token));
    }
    
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _processingTask.WithExceptionLogging(_logger);
    }
    
    private sealed record PiperResponseStream(HttpResponseMessage Response, Stream Stream) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Response.Dispose();
            await Stream.DisposeAsync();
        }
    }
}