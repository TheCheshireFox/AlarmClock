using System;
using AlarmClock.Configuration;
using AlarmClock.Weather;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class OpenWeatherProviderConfig : IOpenWeatherProviderConfig
{
    private readonly IOptionsMonitor<WeatherConfiguration> _weatherConfig;

    public string StateFilePath => PathProvider.GetWeatherStatePath();
    public string ApiKey => _weatherConfig.CurrentValue.OpenWeather.ApiKey;
    public TimeSpan UpdateInterval => _weatherConfig.CurrentValue.UpdateInterval;
    
    public OpenWeatherProviderConfig(IOptionsMonitor<WeatherConfiguration> weatherConfig)
    {
        _weatherConfig = weatherConfig;
    }
}