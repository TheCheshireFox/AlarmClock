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

public sealed class SchedulerBrightnessPolicy(
    IBacklightSchedulerConfig config,
    IService<IDisplayController> displayControllerService,
    ILogger<SchedulerBrightnessPolicy> logger)
    : IBrightnessPolicy, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private Task _actionTask = Task.CompletedTask;

    public bool IsActive { get; private set; }
    
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        Schedule(config.DimStart, StartDim, "start dim");
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
        
        logger.LogInformation("\"{ActionName}\" scheduled to {Target}", actionName, target);
    }

    private void StartDim()
    {
        IsActive = true;
        displayControllerService.Get().Dim(config.DimBrightness);
        Schedule(config.DimStop, StopDim, "stop dim");
    }

    private void StopDim()
    {
        IsActive = false;
        displayControllerService.Get().Dim(1);
        Schedule(config.DimStart, StartDim, "start dim");
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _actionTask.WithExceptionLogging(logger);
    }
}