using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Announcer;

public interface IAnnouncer
{
    public Task SayAsync(string text, CancellationToken cancellationToken);
}