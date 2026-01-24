using AlarmClock.Audio.AudioDevice;
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

public class RadioPlayer : IRadioPlayer
{
    private readonly IRadioListProvider _radioListProvider;
    private readonly IAudioManager _audioManager;
    private readonly AudioPriority _priority;
    private readonly ILogger<RadioPlayer> _logger;

    private (Mpg123RadioAudioSource Source, IAudioSession Session)? _session;
    
    public bool IsPlaying { get; private set; }
    
    public RadioPlayer(IRadioListProvider radioListProvider, IAudioManager audioManager, AudioPriority priority, ILogger<RadioPlayer> logger)
    {
        _radioListProvider = radioListProvider;
        _audioManager = audioManager;
        _priority = priority;
        _logger = logger;
    }
    
    public async Task PlayAsync(string name, CancellationToken cancellationToken)
    {
        if (!_radioListProvider.Get().TryGetValue(name, out var radioUrl))
            throw new Exception($"Radio {name} not found");

        await StopAsync(cancellationToken);

        var source = new Mpg123RadioAudioSource(new Uri(radioUrl));
        var audioSession = await _audioManager.OpenSessionAsync(source, _priority, cancellationToken);
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
        
        _logger.LogInformation("Radio stopped");
    }
}