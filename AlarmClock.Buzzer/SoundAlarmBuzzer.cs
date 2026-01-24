using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioManager;
using AlarmClock.Audio.AudioSource;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Buzzer;

public interface ISoundAlarmBuzzerConfig
{
    string SoundName { get; }
    Stream? OpenSoundStream();
}

public class SoundAlarmBuzzer : IAlarmBuzzer
{
    private readonly ISoundAlarmBuzzerConfig _config;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<SoundAlarmBuzzer> _logger;

    private (IAudioSession Session, SoxWavAudioSource Client)? _audioSession;

    public SoundAlarmBuzzer(ISoundAlarmBuzzerConfig config, IAudioManager audioManager, ILogger<SoundAlarmBuzzer> logger)
    {
        _config = config;
        _audioManager = audioManager;
        _logger = logger;
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        await using var stream = _config.OpenSoundStream();
        if (stream == null)
            throw new Exception($"Alarm {_config.SoundName} not found.");
        
        await StopAsync(cancellationToken);
        
        var client = new SoxWavAudioSource(stream, []);
        var session = await _audioManager.OpenSessionAsync(client, AudioPriority.Exclusive, cancellationToken);
        
        _audioSession = (session, client);
        
        _logger.LogInformation("Started {Name} sound alarm", _config.SoundName);
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