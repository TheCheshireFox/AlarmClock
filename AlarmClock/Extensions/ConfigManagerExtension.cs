using System;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.Extensions;

public static class ConfigManagerExtension
{
    public static void Update<T>(this IConfigManager manager, IOptionsMonitor<T> options, Action<T> update) where T : notnull
    {
        var value = options.CurrentValue;
        update(value);
        manager.Update(value);
    }
}