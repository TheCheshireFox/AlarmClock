using AlarmClock.Audio.AudioDevice;
using AlarmClock.Radio;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Buzzer;

public interface IRadioAlarmBuzzerConfig
{
    string RadioName { get; }
}

public class RadioAlarmBuzzer(
    IRadioAlarmBuzzerConfig config,
    IRadioPlayerFactory radioPlayerFactory,
    ILogger<RadioAlarmBuzzer> logger)
    : IAlarmBuzzer
{
    private readonly IRadioPlayer _radioPlayer = radioPlayerFactory.Create(AudioPriority.Exclusive);

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        await _radioPlayer.PlayAsync(config.RadioName, cancellationToken);
        logger.LogInformation("Alarm buzzer started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _radioPlayer.StopAsync(cancellationToken);
        logger.LogInformation("Alarm buzzer stopped");
    }
}