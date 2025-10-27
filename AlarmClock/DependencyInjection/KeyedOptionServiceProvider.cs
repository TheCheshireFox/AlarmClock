using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlarmClock.DependencyInjection;

public interface IKeyedOptionServiceProvider<out T> where T : notnull
{
    T Get();
}

public class KeyedOptionServiceProvider<T, TOption> : IKeyedOptionServiceProvider<T> where T : notnull
{
    private readonly IOptionsMonitor<TOption> _options;
    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly Func<TOption, object> _keyGetter;

    public KeyedOptionServiceProvider(IOptionsMonitor<TOption> options, IKeyedServiceProvider serviceProvider, Func<TOption, object> keyGetter)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _keyGetter = keyGetter;
    }

    public T Get() => _serviceProvider.GetRequiredKeyedService<T>(_keyGetter(_options.CurrentValue));
}