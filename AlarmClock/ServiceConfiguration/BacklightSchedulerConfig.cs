using System;
using AlarmClock.Configuration;
using AlarmClock.Display.BacklightController.BrightnessPolicy;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class BacklightSchedulerConfig(IOptionsMonitor<BacklightControlConfiguration> options)
    : IBacklightSchedulerConfig
{
    public TimeSpan DimStart => options.CurrentValue.SchedulerPolicy.DimStart;
    public TimeSpan DimStop => options.CurrentValue.SchedulerPolicy.DimStop;
    public double DimBrightness => options.CurrentValue.SchedulerPolicy.DimBrightness;
}