using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Configuration;
using AlarmClock.DisplayController;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock.BacklightController.BrightnessPolicy;

public sealed class SchedulerBrightnessPolicy : IBrightnessPolicy, IAsyncDisposable
{
    private readonly IOptionsMonitor<BacklightControlConfiguration> _options;
    private readonly IDisplayController _displayController;
    private readonly ILogger<SchedulerBrightnessPolicy> _logger;
    private readonly CancellationTokenSource _cts = new();

    private Task _actionTask = Task.CompletedTask;

    public SchedulerBrightnessPolicy(IOptionsMonitor<BacklightControlConfiguration> options, IDisplayController displayController, ILogger<SchedulerBrightnessPolicy> logger)
    {
        _options = options;
        _displayController = displayController;
        _logger = logger;
    }

    public bool IsActive { get; private set; }
    
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        Schedule(_options.CurrentValue.SchedulerPolicy.DimStart, StartDim, "start dim");
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
        _displayController.Dim(_options.CurrentValue.SchedulerPolicy.DimBrightness);
        Schedule(_options.CurrentValue.SchedulerPolicy.DimStop, StopDim, "stop dim");
    }

    private void StopDim()
    {
        IsActive = false;
        _displayController.Dim(1);
        Schedule(_options.CurrentValue.SchedulerPolicy.DimStart, StartDim, "start dim");
    }
    
    public void Dispose()
    {
        _cts.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _actionTask.WithExceptionLogging(_logger);
    }
}