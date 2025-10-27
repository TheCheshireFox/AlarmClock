using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Configuration;
using AlarmClock.Extensions;
using AlarmClock.Shared.Extensions;
using AlarmClock.Weather.OpenWeather;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock.Weather;

public sealed class OpenWeatherProvider : IAsyncDisposable, IWeatherProvider
{
    private static readonly string _statePath = PathProvider.GetWeatherStatePath();

    private readonly IOptionsMonitor<WeatherConfiguration> _options;
    private readonly ILogger<OpenWeatherProvider> _logger;

    private readonly OpenWeatherClient _openWeatherClient;
    private readonly CancellationTokenSource _cts = new();
    private Task _updateTask = Task.CompletedTask;

    public event Action<WeatherInfo>? WeatherUpdated;

    public OpenWeatherProvider(IOptionsMonitor<WeatherConfiguration> options, ILogger<OpenWeatherProvider> logger, ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = logger;
        _openWeatherClient = new OpenWeatherClient(options.CurrentValue.OpenWeather.ApiKey, loggerFactory.CreateLogger<OpenWeatherClient>());
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing...");
        
        var state = await LoadStateAsync(cancellationToken);
        if (state is { Lat: 0, Lon: 0 })
        {
            _logger.LogInformation("Location is unknown, loading...");
            
            if (await TryGetGeolocationAsync(cancellationToken) is {} location)
            {
                state.Lat = location.Lat;
                state.Lon = location.Lon;
                await SaveStateAsync(state, cancellationToken);
            }
        }

        _updateTask = Task.Factory.StartNew(async () => await UpdateWeatherAsync(state), TaskCreationOptions.LongRunning).Unwrap();
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
    
    private async Task UpdateWeatherAsync(WeatherState weatherState)
    {
        var diff = weatherState.LastUpdate + _options.CurrentValue.UpdateInterval - DateTime.Now;
        if (diff < TimeSpan.Zero)
            diff = TimeSpan.Zero;
        
        WeatherUpdated?.Invoke(new WeatherInfo(weatherState.LastTemperature,
            weatherState.LastPressure,
            weatherState.LastHumidity,
            weatherState.LastWeatherCode,
            weatherState.LastDaylight));
        
        _logger.LogDebug("Initial delay {Delay}", diff);
        
        await Task.Delay(diff, _cts.Token);
        
        while (!_cts.IsCancellationRequested)
        {
            if (await _openWeatherClient.TryGetWeatherReportAsync(weatherState.Lat, weatherState.Lon, _cts.Token) is { } weather)
            {
                var weatherCode = weather.Weather.Length > 0 ? weather.Weather[0].WeatherCode : WeatherCode.Unknown;
                var daylight = weather.Weather.Length == 0 || weather.Weather[0].Icon.EndsWith('d');
            
                WeatherUpdated?.Invoke(new WeatherInfo(weather.Main.Temperature,
                    weather.Main.Pressure,
                    weather.Main.HumidityPercent / 100D,
                    weatherCode,
                    daylight));

                weatherState.LastUpdate = DateTime.Now;
                weatherState.LastTemperature = weather.Main.Temperature;
                weatherState.LastPressure = weather.Main.Pressure;
                weatherState.LastHumidity = weather.Main.HumidityPercent / 100D;
                weatherState.LastWeatherCode = weatherCode;
                weatherState.LastDaylight = daylight;
            
                await SaveStateAsync(weatherState, _cts.Token);
            }

            _logger.LogDebug("Next weather update {Time}", DateTime.Now.Add(_options.CurrentValue.UpdateInterval));
            
            await Task.Delay(_options.CurrentValue.UpdateInterval, _cts.Token);
        }
    }
    
    private static async Task<WeatherState> LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!Path.Exists(_statePath))
            return new WeatherState();
        
        await using var stream = File.OpenRead(_statePath);
        return await JsonSerializer.DeserializeAsync<WeatherState>(stream, cancellationToken: cancellationToken) ?? new WeatherState();
    }

    private static async Task SaveStateAsync(WeatherState weatherState, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_statePath);

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
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _updateTask.WithExceptionLogging(_logger);
    }
}