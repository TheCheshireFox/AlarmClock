using AlarmClock.Audio.AudioDevice;
using AlarmClock.Radio;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Buzzer;

public interface IRadioAlarmBuzzerConfig
{
    string RadioName { get; }
}

public class RadioAlarmBuzzer : IAlarmBuzzer
{
    private readonly IRadioAlarmBuzzerConfig _config;
    private readonly IRadioPlayer _radioPlayer;
    private readonly ILogger<RadioAlarmBuzzer> _logger;

    public RadioAlarmBuzzer(IRadioAlarmBuzzerConfig config, IRadioPlayerFactory radioPlayerFactory, ILogger<RadioAlarmBuzzer> logger)
    {
        _config = config;
        _radioPlayer = radioPlayerFactory.Create(AudioPriority.Exclusive);
        _logger = logger;
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        await _radioPlayer.PlayAsync(_config.RadioName, cancellationToken);
        _logger.LogInformation("Alarm buzzer started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _radioPlayer.StopAsync(cancellationToken);
        _logger.LogInformation("Alarm buzzer stopped");
    }
}