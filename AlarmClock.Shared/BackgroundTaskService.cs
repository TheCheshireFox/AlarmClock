using Microsoft.Extensions.Hosting;

namespace AlarmClock.Shared;

public sealed class BackgroundTaskService(Func<CancellationToken, Task> task) : BackgroundService
{
    public bool IsRunning => ExecuteTask is { IsCompleted: false };
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await task(stoppingToken);
}