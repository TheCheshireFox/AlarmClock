using System;
using System.Text.Json.Serialization;

namespace AlarmClock.Weather.OpenWeather;

public record Weather(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("main")] string Main,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("icon")] string Icon)
{
    public WeatherCode WeatherCode { get; } = !Enum.IsDefined((WeatherCode)(Id / 100 * 100)) ? WeatherCode.Unknown : (WeatherCode)(Id / 100 * 100);
}