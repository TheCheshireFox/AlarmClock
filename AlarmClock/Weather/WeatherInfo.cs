using System;
using AlarmClock.Weather.OpenWeather;

namespace AlarmClock.Weather;

public record WeatherInfo(double Temperature, double Pressure, double Humidity, WeatherCode Weather, bool IsDaylight);