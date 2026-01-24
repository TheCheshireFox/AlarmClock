using System.Text.Json.Serialization;

namespace AlarmClock.Weather.OpenWeather;

public record WeatherReport(
    [property: JsonPropertyName("weather")] OpenWeather.Weather[] Weather,
    [property: JsonPropertyName("main")] WeatherMain Main,
    [property: JsonPropertyName("wind")] Wind Wind);