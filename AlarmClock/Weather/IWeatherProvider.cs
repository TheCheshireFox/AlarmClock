using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Weather;

public interface IWeatherProvider
{
    event Action<WeatherInfo>? WeatherUpdated;
    Task InitializeAsync(CancellationToken cancellationToken);
}