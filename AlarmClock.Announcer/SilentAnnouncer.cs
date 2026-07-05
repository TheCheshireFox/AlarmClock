namespace AlarmClock.Announcer;

public class SilentAnnouncer : IAnnouncer
{
    public Task EnqueueSayAsync(string text, CancellationToken cancellationToken) => Task.CompletedTask;
}