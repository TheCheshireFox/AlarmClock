using AlarmClock.Display.DisplayController;
using AlarmClock.Shared;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Display.BacklightController.BrightnessPolicy;

public interface IBacklightSchedulerConfig
{
    TimeSpan DimStart { get; }
    TimeSpan DimStop  { get; }
    double DimBrightness { get; }
}

public sealed class SchedulerBrightnessPolicy : IBrightnessPolicy, IAsyncDisposable
{
    private readonly IBacklightSchedulerConfig _config;
    private readonly IService<IDisplayController> _displayControllerService;
    private readonly ILogger<SchedulerBrightnessPolicy> _logger;
    private readonly CancellationTokenSource _cts = new();

    private Task _actionTask = Task.CompletedTask;

    public SchedulerBrightnessPolicy(IBacklightSchedulerConfig config, IService<IDisplayController> displayControllerService, ILogger<SchedulerBrightnessPolicy> logger)
    {
        _config = config;
        _displayControllerService = displayControllerService;
        _logger = logger;
    }

    public bool IsActive { get; private set; }
    
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        Schedule(_config.DimStart, StartDim, "start dim");
        return Task.CompletedTask;
    }

    private void Schedule(TimeSpan time, Action action, string actionName)
    {
        var target = DateTime.Now.Date.Add(time);
        if (target < DateTime.Now)
            target = target.AddDays(1);
        
        _actionTask = Task.Run(async () =>
        {
            await Task.Delay(DateTime.Now - target, _cts.Token);
            action();
        });
        
        _logger.LogInformation("\"{ActionName}\" scheduled to {Target}", actionName, target);
    }

    private void StartDim()
    {
        IsActive = true;
        _displayControllerService.Get().Dim(_config.DimBrightness);
        Schedule(_config.DimStop, StopDim, "stop dim");
    }

    private void StopDim()
    {
        IsActive = false;
        _displayControllerService.Get().Dim(1);
        Schedule(_config.DimStart, StartDim, "start dim");
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _actionTask.WithExceptionLogging(_logger);
    }
}