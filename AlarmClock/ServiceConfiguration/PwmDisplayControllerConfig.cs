using AlarmClock.Configuration;
using AlarmClock.Display.DisplayController;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class PwmDisplayControllerConfig(IOptionsMonitor<DisplayControllerConfiguration> options) : IPwmDisplayControllerConfig
{
    public int Pin => options.CurrentValue.Pwm.Pin;
    public float Frequency => options.CurrentValue.Pwm.Frequency;
}