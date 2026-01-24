using System;
using AlarmClock.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlarmClock.DependencyInjection;

public class KeyedOptionService<T, TOption> : IService<T> where T : notnull
{
    private readonly IOptionsMonitor<TOption> _options;
    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly Func<TOption, object> _keyGetter;

    public KeyedOptionService(IOptionsMonitor<TOption> options, IKeyedServiceProvider serviceProvider, Func<TOption, object> keyGetter)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _keyGetter = keyGetter;
    }

    public T Get() => _serviceProvider.GetRequiredKeyedService<T>(_keyGetter(_options.CurrentValue));
}