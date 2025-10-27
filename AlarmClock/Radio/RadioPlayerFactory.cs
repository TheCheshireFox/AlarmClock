using AlarmClock.AlarmBuzzer;
using AlarmClock.Audio.AudioDevice;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Radio;

public interface IRadioPlayerFactory
{
    IRadioPlayer Create(AudioPriority priority);
}

public class RadioPlayerFactory : IRadioPlayerFactory
{
    private readonly IRadioListProvider _radioListProvider;
    private readonly IAudioDevice _audioDevice;
    private readonly ILogger<RadioAlarmBuzzer> _logger;

    public RadioPlayerFactory(IRadioListProvider radioListProvider, IAudioDevice audioDevice, ILogger<RadioAlarmBuzzer> logger)
    {
        _radioListProvider = radioListProvider;
        _audioDevice = audioDevice;
        _logger = logger;
    }
    
    public IRadioPlayer Create(AudioPriority priority) => new RadioPlayer(_radioListProvider, _audioDevice, priority, _logger);
}