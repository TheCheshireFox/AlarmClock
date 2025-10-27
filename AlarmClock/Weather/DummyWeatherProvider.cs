using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Weather;

public class DummyWeatherProvider : IWeatherProvider
{
    public event Action<WeatherInfo>? WeatherUpdated { add { } remove { } }
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}