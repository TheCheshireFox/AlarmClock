using System.Text.Json;
using System.Text.Json.Nodes;
using AlarmClock.Weather.OpenWeather;
using Microsoft.Extensions.Logging;
namespace AlarmClock.Weather;

public interface IOpenWeatherProviderConfig
{
    public string StateFilePath { get; }
    public string ApiKey { get; }
    public TimeSpan UpdateInterval { get; }
}

public sealed class OpenWeatherProvider : IWeatherProvider
{
    private readonly IOpenWeatherProviderConfig _config;
    private readonly ILogger<OpenWeatherProvider> _logger;

    private readonly OpenWeatherClient _openWeatherClient;

    private WeatherState _currentState = new();

    public OpenWeatherProvider(IOpenWeatherProviderConfig config, ILogger<OpenWeatherProvider> logger, ILoggerFactory loggerFactory)
    {
        _config = config;
        _logger = logger;
        _openWeatherClient = new OpenWeatherClient(_config.ApiKey, loggerFactory.CreateLogger<OpenWeatherClient>());
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing...");
        
        _currentState = await LoadStateAsync(cancellationToken);
        if (_currentState is { Lat: 0, Lon: 0 })
        {
            _logger.LogInformation("Location is unknown, loading...");
            
            if (await TryGetGeolocationAsync(cancellationToken) is {} location)
            {
                _currentState.Lat = location.Lat;
                _currentState.Lon = location.Lon;
                await SaveStateAsync(_currentState, cancellationToken);
            }
        }
    }

    public async Task<WeatherInfo> GetWeatherAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _currentState.LastUpdate < _config.UpdateInterval)
            return _currentState.ToWeatherInfo();

        if (await _openWeatherClient.TryGetWeatherReportAsync(_currentState.Lat, _currentState.Lon, cancellationToken) is not { } weather)
        {
            _logger.LogWarning("Failed to get weather report");
            return _currentState.ToWeatherInfo();
        }
        
        var weatherCode = weather.Weather.Length > 0 ? weather.Weather[0].WeatherCode : WeatherCode.Unknown;
        var daylight = weather.Weather.Length == 0 || weather.Weather[0].Icon.EndsWith('d');

        _currentState.LastUpdate = DateTime.UtcNow;
        _currentState.LastTemperature = weather.Main.Temperature;
        _currentState.LastPressure = weather.Main.Pressure;
        _currentState.LastHumidity = weather.Main.HumidityPercent / 100D;
        _currentState.LastWeatherCode = weatherCode;
        _currentState.LastDaylight = daylight;
            
        await SaveStateAsync(_currentState, cancellationToken);

        return _currentState.ToWeatherInfo();
    }

    private async Task<(double Lat, double Lon)?> TryGetGeolocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            var resp = JsonSerializer.Deserialize<JsonObject>(await httpClient.GetStringAsync("https://ip-api.com/json/", cancellationToken))!;
            
            var lat = resp["lat"]!.GetValue<double>();
            var lon = resp["lon"]!.GetValue<double>();
            var city = resp["city"]!.GetValue<string>();

            _logger.LogInformation("Location is set to {City}. Lat: {Lat}, Lon: {Lon}", city, lat, lon);
            
            return (lat, lon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to get geolocation");
            return null;
        }
    }

    private async Task<WeatherState> LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!Path.Exists(_config.StateFilePath))
            return new WeatherState();
        
        await using var stream = File.OpenRead(_config.StateFilePath);
        return await JsonSerializer.DeserializeAsync<WeatherState>(stream, cancellationToken: cancellationToken) ?? new WeatherState();
    }

    private async Task SaveStateAsync(WeatherState weatherState, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_config.StateFilePath);

        await JsonSerializer.SerializeAsync(stream, weatherState, cancellationToken: cancellationToken);
    }
    
    private class WeatherState
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;
        public double LastTemperature { get; set; }
        public double LastPressure { get; set; }
        public double LastHumidity { get; set; }
        public WeatherCode LastWeatherCode { get; set; } = WeatherCode.Unknown;
        public bool LastDaylight { get; set; } = true;

        public WeatherInfo ToWeatherInfo() => new(LastTemperature,
            LastPressure,
            LastHumidity,
            LastWeatherCode,
            LastDaylight);
    }
}