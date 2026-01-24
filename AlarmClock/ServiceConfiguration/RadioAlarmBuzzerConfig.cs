using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class RadioAlarmBuzzerConfig : IRadioAlarmBuzzerConfig
{
    private readonly IOptionsMonitor<BuzzerConfiguration> _options;

    public string RadioName => _options.CurrentValue.Radio.Name;
    
    public RadioAlarmBuzzerConfig(IOptionsMonitor<BuzzerConfiguration> options)
    {
        _options = options;
    }
}