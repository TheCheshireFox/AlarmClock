using AlarmClock.Display.BacklightController.BrightnessPolicy;
using AlarmClock.Display.DisplayController;
using AlarmClock.Shared;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace AlarmClock.Display.BacklightController;

public interface IBacklightControllerConfig
{
    IService<IDisplayController> DisplayControllerService { get; }
    IService<IBrightnessPolicy> BrightnessPolicyService { get; }
    TimeSpan? DimTimeout { get; }
    TimeSpan? StandbyTimeout { get; }
    double DimLevel { get; }
}

public interface IBacklightController
{
    Task StartAsync(CancellationToken cancellationToken);
    Task ResetAsync(CancellationToken cancellationToken);
    Task<bool> EnableAsync(bool enable, CancellationToken cancellationToken);
}

public class BacklightController : IBacklightController
{
    private readonly IBacklightControllerConfig _config;
    private readonly ILogger<BacklightController> _logger;
    private readonly Timer? _dimTimer;
    private readonly Timer? _standbyTimer;

    public BacklightController(IBacklightControllerConfig config, ILogger<BacklightController> logger)
    {
        _config = config;
        _logger = logger;

        _logger.LogInformation("Initializing...");

        if (_config.DimTimeout is {} dimTimeout)
        {
            _dimTimer = CreateDisplayTimer(dimTimeout, OnDim);
            _logger.LogInformation("Dim timeout: {Timeout}", dimTimeout);
        }

        if (_config.StandbyTimeout is {} standbyTimeout)
        {
            _standbyTimer = CreateDisplayTimer(standbyTimeout, OnStandby);
            _logger.LogInformation("Standby timeout: {Timeout}", standbyTimeout);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        StartTimers();
        await _config.BrightnessPolicyService.Get().InitializeAsync(cancellationToken);
    }

    public Task ResetAsync(CancellationToken cancellationToken)
    {
        _dimTimer?.Stop();
        _standbyTimer?.Stop();
        
        _config.DisplayControllerService.Get().On(true);
        _config.DisplayControllerService.Get().Dim(1);
        
        StartTimers();
        
        return Task.CompletedTask;
    }

    public async Task<bool> EnableAsync(bool enable, CancellationToken cancellationToken)
    {
        if (!enable)
        {
            _dimTimer?.Stop();
            _standbyTimer?.Stop();
            return _config.DisplayControllerService.Get().On(false);
        }
        
        if (!_config.DisplayControllerService.Get().On(true))
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
        if (_config.BrightnessPolicyService.Get().IsActive)
            return;

        _config.DisplayControllerService.Get().Dim(_config.DimLevel);
        _dimTimer?.Stop();
        _standbyTimer?.Start();
    }

    private void OnStandby()
    {
        if (_config.BrightnessPolicyService.Get().IsActive)
            return;

        _config.DisplayControllerService.Get().On(false);
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