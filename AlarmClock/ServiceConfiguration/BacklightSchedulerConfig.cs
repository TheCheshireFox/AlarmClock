using System;
using AlarmClock.Configuration;
using AlarmClock.Display.BacklightController.BrightnessPolicy;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class BacklightSchedulerConfig : IBacklightSchedulerConfig
{
    private readonly IOptionsMonitor<BacklightControlConfiguration> _options;

    public TimeSpan DimStart => _options.CurrentValue.SchedulerPolicy.DimStart;
    public TimeSpan DimStop => _options.CurrentValue.SchedulerPolicy.DimStop;
    public double DimBrightness => _options.CurrentValue.SchedulerPolicy.DimBrightness;
    
    public BacklightSchedulerConfig(IOptionsMonitor<BacklightControlConfiguration> options)
    {
        _options = options;
    }
}