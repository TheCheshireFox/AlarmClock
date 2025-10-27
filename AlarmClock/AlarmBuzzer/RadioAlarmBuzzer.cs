using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Configuration;
using AlarmClock.Radio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock.AlarmBuzzer;

public class RadioAlarmBuzzer : IAlarmBuzzer
{
    private readonly IOptionsMonitor<BuzzerConfiguration> _options;
    private readonly IRadioPlayer _radioPlayer;
    private readonly ILogger<RadioAlarmBuzzer> _logger;

    public RadioAlarmBuzzer(IOptionsMonitor<BuzzerConfiguration> options, IRadioPlayerFactory radioPlayerFactory, ILogger<RadioAlarmBuzzer> logger)
    {
        _options = options;
        _radioPlayer = radioPlayerFactory.Create(AudioPriority.Exclusive);
        _logger = logger;
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        await _radioPlayer.PlayAsync(_options.CurrentValue.Radio.Name, cancellationToken);
        _logger.LogInformation("Alarm buzzer started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _radioPlayer.StopAsync(cancellationToken);
        _logger.LogInformation("Alarm buzzer stopped");
    }
}