using System.Text.Json.Serialization;

namespace AlarmClock.Weather.OpenWeather;

public record WeatherMain(
    [property: JsonPropertyName("temp")] double Temperature,
    [property: JsonPropertyName("feels_like")] double FeelsLike,
    [property: JsonPropertyName("temp_min")] double MinTemperature,
    [property: JsonPropertyName("temp_max")] double MaxTemperature,
    [property: JsonPropertyName("pressure")] int Pressure,
    [property: JsonPropertyName("humidity")] int HumidityPercent,
    [property: JsonPropertyName("sea_level")] int PressureSeaLevel,
    [property: JsonPropertyName("grnd_level")] int PressureGroundLevel);