using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class GpioAlarmBuzzerConfig(IOptionsMonitor<BuzzerConfiguration> options) : IGpioBuzzerConfig
{
    public int Pin => options.CurrentValue.Gpio.Pin;
}
