namespace AlarmClock.Shared.Extensions;

public static class SemaphoreSlimExtension
{
    private sealed class LockReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
    
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken = default)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        return new LockReleaser(semaphoreSlim);
    }
}