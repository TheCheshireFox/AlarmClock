using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Announcer;

public interface IAnnouncer
{
    public Task EnqueueSayAsync(string text, CancellationToken cancellationToken);
}