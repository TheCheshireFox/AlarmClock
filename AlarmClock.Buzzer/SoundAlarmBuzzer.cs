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

public class SoundAlarmBuzzer(
    ISoundAlarmBuzzerConfig config,
    IAudioManager audioManager,
    ILogger<SoundAlarmBuzzer> logger)
    : IAlarmBuzzer
{
    private (IAudioSession Session, SoxWavAudioSource Client)? _audioSession;

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        await using var stream = config.OpenSoundStream();
        if (stream == null)
            throw new Exception($"Alarm {config.SoundName} not found.");
        
        await StopAsync(cancellationToken);
        
        var client = new SoxWavAudioSource(stream, []);
        var session = await audioManager.OpenSessionAsync(client, AudioPriority.Exclusive, cancellationToken);
        
        _audioSession = (session, client);
        
        logger.LogInformation("Started {Name} sound alarm", config.SoundName);
    }

    public async Task StopAsync(CancellationToken cancellation)
    {
        if (_audioSession is not {} audioSession)
            return;

        await audioSession.Session.DisposeAsync();
        await audioSession.Client.DisposeAsync();

        _audioSession = null;
        
        logger.LogInformation("Sound alarm stopped");
    }
}