using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Weather.OpenWeather;

public interface IOpenWeatherClient
{
    Task<WeatherReport?> TryGetWeatherReportAsync(double lat, double lon, CancellationToken cancellationToken);
}