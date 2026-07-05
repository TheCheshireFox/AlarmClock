using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Weather.OpenWeather;

public class OpenWeatherClient(string apiKey, ILogger<OpenWeatherClient> logger)
{
    private readonly HttpClient _httpClient = new();

    public async Task<WeatherReport?> TryGetWeatherReportAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _httpClient.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric", cancellationToken);
            return JsonSerializer.Deserialize<WeatherReport>(json) ?? throw new Exception("Invalid weather report json format");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to get weather report for {lat}, {lon}", lat, lon);
            return null;
        }
    }
}