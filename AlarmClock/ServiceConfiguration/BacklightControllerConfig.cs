using System;
using AlarmClock.Configuration;
using AlarmClock.Display.BacklightController;
using AlarmClock.Display.BacklightController.BrightnessPolicy;
using AlarmClock.Display.DisplayController;
using AlarmClock.Shared;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class BacklightControllerConfig : IBacklightControllerConfig
{
    private readonly IOptionsMonitor<BacklightControlConfiguration> _options;
    
    public IService<IDisplayController> DisplayControllerService { get; }
    public IService<IBrightnessPolicy> BrightnessPolicyService { get; }
    public TimeSpan? DimTimeout => _options.CurrentValue.DimTimeout;
    public TimeSpan? StandbyTimeout =>  _options.CurrentValue.StandbyTimeout;
    public double DimLevel =>  _options.CurrentValue.DimLevel;
    
    public BacklightControllerConfig(IService<IDisplayController> displayControllerServiceService, IService<IBrightnessPolicy> brightnessPolicyService, IOptionsMonitor<BacklightControlConfiguration> options)
    {
        _options = options;
        DisplayControllerService = displayControllerServiceService;
        BrightnessPolicyService = brightnessPolicyService;
    }
}