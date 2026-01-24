using System;
using System.Reactive.Disposables;
using Microsoft.Extensions.Options;

namespace AlarmClock.Extensions;

public static class OptionsMonitorExtension
{
    public static IDisposable Subscribe<TOptions>(this IOptionsMonitor<TOptions> monitor, Action<TOptions> action)
    {
        return monitor.OnChange(action) ?? Disposable.Empty;
    }
}