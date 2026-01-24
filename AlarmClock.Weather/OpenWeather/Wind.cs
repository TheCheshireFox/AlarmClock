using System.Text.Json.Serialization;

namespace AlarmClock.Weather.OpenWeather;

public record Wind(
    [property: JsonPropertyName("speed")] double Speed,
    [property: JsonPropertyName("deg")] int DirectionDegrees,
    [property: JsonPropertyName("gust")] double Gust);