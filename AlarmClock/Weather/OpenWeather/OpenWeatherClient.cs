using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Weather.OpenWeather;

public class OpenWeatherClient : IOpenWeatherClient
{
    private readonly string _apiKey;
    private readonly ILogger<OpenWeatherClient> _logger;
    private readonly HttpClient _httpClient = new();

    public OpenWeatherClient(string apiKey, ILogger<OpenWeatherClient> logger)
    {
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<WeatherReport?> TryGetWeatherReportAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _httpClient.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric", cancellationToken);
            return JsonSerializer.Deserialize<WeatherReport>(json) ?? throw new Exception("Invalid weather report json format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to get weather report for {lat}, {lon}", lat, lon);
            return null;
        }
    }
}