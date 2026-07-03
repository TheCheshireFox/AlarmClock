using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class RadioAlarmBuzzerConfig(IOptionsMonitor<BuzzerConfiguration> options) : IRadioAlarmBuzzerConfig
{
    public string RadioName => options.CurrentValue.Radio.Name;
}