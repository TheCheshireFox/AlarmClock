using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.AlarmBuzzer;
using AlarmClock.DependencyInjection;
using AlarmClock.Radio;
using AlarmClock.Weather;

namespace AlarmClock.Design;

public static class AppService
{
    private static readonly Dictionary<Type, object> _services = new();

#if DEBUG
    static AppService()
    {
        GetDefault<IAlarmService>(x => x.GetAlarmAsync(CancellationToken.None) == Task.FromResult(new AlarmSettings(false, TimeSpan.Zero)));
        GetDefault<IKeyedOptionServiceProvider<IWeatherProvider>>(x => x.Get() == GetDefault<IWeatherProvider>());
        GetDefault<IRadioListProvider>(x => x.Get() == new Dictionary<string, string>{ {"test1", "test"}, {"test2", "test"} });
        GetDefault<IAlarmListProvider>(x => x.Get() == new Dictionary<string, string>{ {"test1", "test"}, {"test2", "test"} });
    }
#endif

    public static T GetDefault<T>(params Expression<Func<T, object>>[] expressions) where T : class
    {
#if DEBUG
        if (!Avalonia.Controls.Design.IsDesignMode)
            return null!;

        if (!_services.TryGetValue(typeof(T), out var obj))
            _services.Add(typeof(T), obj = DefaultProxy<T>.Create(expressions));

        return (T)obj;
#else
        return null!;
#endif
    }
}