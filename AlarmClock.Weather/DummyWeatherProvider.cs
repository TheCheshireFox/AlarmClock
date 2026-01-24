using AlarmClock.Weather.OpenWeather;

namespace AlarmClock.Weather;

public class DummyWeatherProvider : IWeatherProvider
{
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<WeatherInfo> GetWeatherAsync(CancellationToken cancellationToken) => Task.FromResult(new WeatherInfo(0, 0, 0, WeatherCode.Clear, true));
}