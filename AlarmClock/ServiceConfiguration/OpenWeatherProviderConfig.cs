using System;
using AlarmClock.Configuration;
using AlarmClock.Weather;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class OpenWeatherProviderConfig(IOptionsMonitor<WeatherConfiguration> weatherConfig) : IOpenWeatherProviderConfig
{
    public string StateFilePath => PathProvider.GetWeatherStatePath();
    public string ApiKey => weatherConfig.CurrentValue.OpenWeather.ApiKey;
    public TimeSpan UpdateInterval => weatherConfig.CurrentValue.UpdateInterval;
}