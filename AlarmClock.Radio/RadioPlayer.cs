using AlarmClock.Audio.AudioManager;
using AlarmClock.Audio.AudioSource;
using Microsoft.Extensions.Logging;
using AudioPriority = AlarmClock.Audio.AudioDevice.AudioPriority;

namespace AlarmClock.Radio;

public interface IRadioPlayer
{
    bool IsPlaying { get; }
    Task PlayAsync(string name, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class RadioPlayer(
    IRadioListProvider radioListProvider,
    IAudioManager audioManager,
    AudioPriority priority,
    ILogger<RadioPlayer> logger)
    : IRadioPlayer
{
    private (Mpg123RadioAudioSource Source, IAudioSession Session)? _session;
    
    public bool IsPlaying { get; private set; }

    public async Task PlayAsync(string name, CancellationToken cancellationToken)
    {
        if (!radioListProvider.Get().TryGetValue(name, out var radioUrl))
            throw new Exception($"Radio {name} not found");

        await StopAsync(cancellationToken);

        var source = new Mpg123RadioAudioSource(new Uri(radioUrl));
        var audioSession = await audioManager.OpenSessionAsync(source, priority, cancellationToken);
        _session = (source, audioSession);

        IsPlaying = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_session is not {} session)
            return;

        await session.Source.DisposeAsync();
        await session.Session.DisposeAsync();

        _session = null;

        IsPlaying = false;
        
        logger.LogInformation("Radio stopped");
    }
}