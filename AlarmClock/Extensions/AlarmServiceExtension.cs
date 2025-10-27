using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Extensions;

public static class AlarmServiceExtension
{
    public static Task StartAsync(this IAlarmService service, TimeSpan time, CancellationToken cancellationToken)
        => service.StartAsync(time.Hours, time.Minutes, cancellationToken);
}