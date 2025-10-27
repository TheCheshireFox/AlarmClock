using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Announcer;

public class SilentAnnouncer : IAnnouncer
{
    public Task SayAsync(string text, CancellationToken cancellationToken) => Task.CompletedTask;
}