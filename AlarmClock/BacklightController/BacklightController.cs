using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.BacklightController.BrightnessPolicy;
using AlarmClock.Configuration;
using AlarmClock.DependencyInjection;
using AlarmClock.DisplayController;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Timer = System.Timers.Timer;

namespace AlarmClock.BacklightController;

public interface IBacklightController
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ResetAsync(CancellationToken cancellationToken);
    Task<bool> EnableAsync(bool enable, CancellationToken cancellationToken);
}

public class BacklightController : IBacklightController
{
    private readonly IOptionsMonitor<BacklightControlConfiguration> _options;
    private readonly IKeyedOptionServiceProvider<IDisplayController> _displayControllerProvider;
    private readonly IKeyedOptionServiceProvider<IBrightnessPolicy> _brightnessPolicyProvider;
    private readonly ILogger<BacklightController> _logger;
    private readonly Timer? _dimTimer;
    private readonly Timer? _standbyTimer;

    public BacklightController(IOptionsMonitor<BacklightControlConfiguration> options,
        IKeyedOptionServiceProvider<IDisplayController> displayControllerProvider,
        IKeyedOptionServiceProvider<IBrightnessPolicy> brightnessPolicyProvider,
        ILogger<BacklightController> logger)
    {
        _options = options;
        _displayControllerProvider = displayControllerProvider;
        _brightnessPolicyProvider = brightnessPolicyProvider;
        _logger = logger;

        _logger.LogInformation("Initializing...");

        if (_options.CurrentValue is { DimTimeout: { } dimTimeout })
        {
            _dimTimer = CreateDisplayTimer(dimTimeout, OnDim);
            _logger.LogInformation("Dim timeout: {Timeout}", dimTimeout);
        }

        if (_options.CurrentValue is { StandbyTimeout: { } standbyTimeout })
        {
            _standbyTimer = CreateDisplayTimer(standbyTimeout, OnStandby);
            _logger.LogInformation("Standby timeout: {Timeout}", standbyTimeout);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        StartTimers();
        await _brightnessPolicyProvider.Get().InitializeAsync(cancellationToken);
    }

    public Task ResetAsync(CancellationToken cancellationToken)
    {
        _dimTimer?.Stop();
        _standbyTimer?.Stop();
        
        var displayController = _displayControllerProvider.Get();
        displayController.On(true);
        displayController.Dim(1);
        
        StartTimers();
        
        return Task.CompletedTask;
    }

    public async Task<bool> EnableAsync(bool enable, CancellationToken cancellationToken)
    {
        var displayController = _displayControllerProvider.Get();
        
        if (!enable)
        {
            _dimTimer?.Stop();
            _standbyTimer?.Stop();
            return displayController.On(false);
        }
        
        if (!displayController.On(true))
            return false;

        await ResetAsync(cancellationToken);
        return true;
    }

    private void StartTimers()
    {
        if (_dimTimer != null)
            _dimTimer.Start();
        else if (_standbyTimer != null)
            _standbyTimer.Start();
    }
    
    private void OnDim()
    {
        if (_brightnessPolicyProvider.Get().IsActive)
            return;
     
        var displayController = _displayControllerProvider.Get();
        
        displayController.Dim(_options.CurrentValue.DimLevel);
        _dimTimer?.Stop();
        _standbyTimer?.Start();
    }

    private void OnStandby()
    {
        if (_brightnessPolicyProvider.Get().IsActive)
            return;
        
        var displayController = _displayControllerProvider.Get();
        
        displayController.On(false);
    }
    
    private static Timer CreateDisplayTimer(TimeSpan interval, Action action)
    {
        var timer = new Timer(interval.TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = false
        };
        timer.Elapsed += (_, _) => action();
        
        return timer;
    }
}