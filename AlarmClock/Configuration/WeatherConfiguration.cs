using System;

namespace AlarmClock.Configuration;

public enum WeatherProviderType
{
    None,
    OpenWeather
}

[ConfigurationPath("weather")]
public class WeatherConfiguration
{
    [TypeVariant(nameof(WeatherProviderType.None))]
    [TypeVariant(nameof(WeatherProviderType.OpenWeather))]
    public WeatherProviderType Type { get; set; } = WeatherProviderType.OpenWeather;
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(10);

    public OpenWeatherConfiguration OpenWeather { get; set; } = new();
}

public class OpenWeatherConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
}