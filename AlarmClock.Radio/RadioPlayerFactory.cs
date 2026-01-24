using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioManager;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Radio;

public interface IRadioPlayerFactory
{
    IRadioPlayer Create(AudioPriority priority);
}

public class RadioPlayerFactory : IRadioPlayerFactory
{
    private readonly IRadioListProvider _radioListProvider;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<RadioPlayer> _logger;

    public RadioPlayerFactory(IRadioListProvider radioListProvider, IAudioManager audioManager, ILogger<RadioPlayer> logger)
    {
        _radioListProvider = radioListProvider;
        _audioManager = audioManager;
        _logger = logger;
    }
    
    public IRadioPlayer Create(AudioPriority priority) => new RadioPlayer(_radioListProvider, _audioManager, priority, _logger);
}