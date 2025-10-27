using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.DependencyInjection;
using AlarmClock.Design;
using AlarmClock.Utility;
using AlarmClock.Weather;
using AlarmClock.Weather.OpenWeather;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using Avalonia.Threading;

namespace AlarmClock.Components;

public partial class Clock : UserControl
{
    public static readonly StyledProperty<string> WeatherIconPathProperty = AvaloniaProperty.Register<Clock, string>(nameof(WeatherIconPath), "/Assets/Images/Weather/clear_night.svg");
    
    public string WeatherIconPath
    {
        get => GetValue(WeatherIconPathProperty);
        set => SetValue(WeatherIconPathProperty, value);
    }
    
    [Inject]
    private IKeyedOptionServiceProvider<IWeatherProvider> WeatherProvider { get; set; } = AppService.GetDefault<IKeyedOptionServiceProvider<IWeatherProvider>>();
    
    public Clock()
    {
        InitializeComponent();

        _ = Task.Run(ClockTick);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        WeatherProvider.Get().WeatherUpdated += weatherInfo =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                WeatherIconPath = GetWeatherIconPath(weatherInfo.Weather, weatherInfo.IsDaylight);
                Temperature.Text = $"{weatherInfo.Temperature:F1}°C";
            });
        };
    }

    private async Task ClockTick()
    {
        while (!ApplicationCancellation.Token.IsCancellationRequested)
        {
            await Task.Delay(1000, ApplicationCancellation.Token);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var now = DateTime.Now;

                TimeText.Text = now.ToString("HH:mm:ss");
                DateText.Text = now.ToString("ddd dd/MM");
            });
        }
    }

    private static string GetWeatherIconPath(WeatherCode code, bool isDaylight)
    {
        return code switch
        {
            WeatherCode.Thunderstorm => GetIconPath("thundestorm.svg"),
            WeatherCode.Snow => GetIconPath("snow.svg"),
            WeatherCode.Drizzle => GetIconPath("shower_rain.svg"),
            WeatherCode.Rain => GetIconPath(isDaylight ? "rain_day.svg" : "rain_nigh.svg"),
            WeatherCode.Atmosphere => GetIconPath("mist.svg"),
            WeatherCode.Clear => GetIconPath(isDaylight ? "clear_day.svg" : "clear_night.svg"),
            WeatherCode.FewClouds => GetIconPath(isDaylight ? "few_clouds_day.svg" : "few_clouds_night.svg"),
            WeatherCode.ScatteredClouds or WeatherCode.BrokenClouds or WeatherCode.OvercastClouds => GetIconPath("clouds.svg"),
            _ => GetIconPath(isDaylight ? "clear_day.svg" : "clear_night.svg")
        };

        string GetIconPath(string name) => $"/Assets/Images/Weather/{name}";
    }
}