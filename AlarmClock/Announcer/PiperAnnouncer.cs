using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Audio;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Configuration;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock.Announcer;

public sealed class PiperAnnouncer : IAnnouncer, IAsyncDisposable
{
    private readonly IAudioDevice _audioDevice;
    private readonly IOptionsMonitor<AnnouncerConfiguration> _options;
    private readonly ILogger<PiperAnnouncer> _logger;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly BlockingCollection<string> _phrases = new();

    public PiperAnnouncer(IAudioDevice audioDevice, IOptionsMonitor<AnnouncerConfiguration> options, ILogger<PiperAnnouncer> logger)
    {
        _audioDevice = audioDevice;
        _options = options;
        _logger = logger;
        _processingTask = Task.Factory.StartNew(ProcessPhrases,  TaskCreationOptions.LongRunning).Unwrap();
    }
    
    public Task SayAsync(string text, CancellationToken cancellationToken)
    {
        _phrases.Add(text, cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ProcessPhrases()
    {
        using var http = new HttpClient();
        
        foreach (var phrase in _phrases.GetConsumingEnumerable(_cts.Token))
        {
            var req = new HttpRequestMessage(HttpMethod.Post, _options.CurrentValue.Piper.Url)
            {
                Content = new StringContent($"{{\"text\":\"{phrase}\"}}", new MediaTypeHeaderValue("application/json"))
            };

            var completionOption = _options.CurrentValue.Piper.Prefetch
                ? HttpCompletionOption.ResponseContentRead
                : HttpCompletionOption.ResponseHeadersRead;
            
            using var resp = await http.SendAsync(req, completionOption, _cts.Token);
            resp.EnsureSuccessStatusCode();

            await using var source = new SoxWavAudioSource(await resp.Content.ReadAsStreamAsync(_cts.Token), ["phaser", "0.5", "1.2", "1.5", "0.7", "2", "chorus", "0.7", "0.9", "55", "0.4", "0.25", "2", "-t"]);
            await using var session  = await _audioDevice.OpenSessionAsync(source, AudioPriority.High, _cts.Token);

            await session.WaitAsync(_cts.Token);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _processingTask.WithExceptionLogging(_logger);
    }
}