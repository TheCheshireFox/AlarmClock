using Microsoft.Extensions.Logging;

namespace AlarmClock.Shared.Extensions;

public static class TaskExtension
{
    public static void ThrowIfFailed(this Task task)
    {
        if (task is { IsFaulted: true, Exception: not null })
            throw task.Exception;
    }
    
    public static async Task WithExceptionLogging(this Task task, ILogger logger, bool propagate = false)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // NOP
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Task failed");

            if (propagate)
                throw;
        }
    }
    
    public static async Task WithExceptionLogging(this Task task, bool propagate = false)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // NOP
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Task failed: {ex}");

            if (propagate)
                throw;
        }
    }
}