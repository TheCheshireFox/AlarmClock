using Microsoft.Extensions.Logging;

namespace AlarmClock.Shared.Extensions;

public static class TaskExtension
{
    extension(Task task)
    {
        public void ThrowIfFailed()
        {
            if (task is { IsFaulted: true, Exception: not null })
                throw task.Exception;
        }

        public async Task WithExceptionLogging(ILogger logger, bool propagate = false)
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

        public async Task WithExceptionLogging(bool propagate = false)
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
}