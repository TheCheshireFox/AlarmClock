using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.AlarmBuzzer;

public interface IAlarmBuzzer
{
    Task PlayAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}