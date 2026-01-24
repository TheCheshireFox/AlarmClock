namespace AlarmClock.Weather;

public interface IWeatherProvider
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<WeatherInfo> GetWeatherAsync(CancellationToken cancellationToken);
}