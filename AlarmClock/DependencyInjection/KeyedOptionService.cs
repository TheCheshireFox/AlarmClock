using System;
using AlarmClock.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlarmClock.DependencyInjection;

public class KeyedOptionService<T, TOption>(
    IOptionsMonitor<TOption> options,
    IKeyedServiceProvider serviceProvider,
    Func<TOption, object> keyGetter)
    : IService<T>
    where T : notnull
{
    public T Get() => serviceProvider.GetRequiredKeyedService<T>(keyGetter(options.CurrentValue));
}