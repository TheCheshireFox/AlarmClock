using System;
using AlarmClock.Configuration;
using AlarmClock.Display.BacklightController;
using AlarmClock.Display.BacklightController.BrightnessPolicy;
using AlarmClock.Display.DisplayController;
using AlarmClock.Shared;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class BacklightControllerConfig(
    IService<IDisplayController> displayControllerServiceService,
    IService<IBrightnessPolicy> brightnessPolicyService,
    IOptionsMonitor<BacklightControlConfiguration> options)
    : IBacklightControllerConfig
{
    public IService<IDisplayController> DisplayControllerService { get; } = displayControllerServiceService;
    public IService<IBrightnessPolicy> BrightnessPolicyService { get; } = brightnessPolicyService;
    public TimeSpan? DimTimeout => options.CurrentValue.DimTimeout;
    public TimeSpan? StandbyTimeout =>  options.CurrentValue.StandbyTimeout;
    public double DimLevel =>  options.CurrentValue.DimLevel;
}