using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Audio;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Configuration;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock.AlarmBuzzer;

public class SoundAlarmBuzzer : IAlarmBuzzer
{
    private readonly IOptionsMonitor<BuzzerConfiguration> _options;
    private readonly IAudioDevice _audioDevice;
    private readonly ILogger<SoundAlarmBuzzer> _logger;

    private (IAudioSession Session, SoxWavAudioSource Client)? _audioSession;

    public SoundAlarmBuzzer(IOptionsMonitor<BuzzerConfiguration> options, IAudioDevice audioDevice, ILogger<SoundAlarmBuzzer> logger)
    {
        _options = options;
        _audioDevice = audioDevice;
        _logger = logger;
    }


    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        var uri = new Uri($"avares://AlarmClock/Assets/Sounds/{_options.CurrentValue.Sound.Name}.wav");

        if (!AssetLoader.Exists(uri))
            throw new Exception($"Alarm {_options.CurrentValue.Sound.Name} not found.");

        await StopAsync(cancellationToken);

        var stream = AssetLoader.Open(uri);
        var client = new SoxWavAudioSource(stream, []);
        var session = await _audioDevice.OpenSessionAsync(client, AudioPriority.Exclusive, cancellationToken);
        
        _audioSession = (session, client);
        
        _logger.LogInformation("Started {Name} sound alarm", _options.CurrentValue.Sound.Name);
    }

    public async Task StopAsync(CancellationToken cancellation)
    {
        if (_audioSession is not {} audioSession)
            return;

        await audioSession.Session.DisposeAsync();
        await audioSession.Client.DisposeAsync();

        _audioSession = null;
        
        _logger.LogInformation("Sound alarm stopped");
    }
}