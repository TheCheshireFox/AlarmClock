using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AlarmClock.Shared;
using AlarmClock.Weather;
using AlarmClock.Weather.OpenWeather;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class ClockViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
{
    public ViewModelActivator Activator { get; }

    public string UrlPathSegment { get; } = nameof(ClockViewModel);
    
    public IScreen HostScreen { get; }

    public DateTime CurrentTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IImage? WeatherIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double Temperature
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ClockViewModel(IScreen screen, IService<IWeatherProvider> weatherService, ILogger<ClockViewModel> logger)
    {
        HostScreen = screen;
        Activator = new ViewModelActivator();
        
        this.WhenActivated(disposables =>
        {
            Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ => CurrentTime = DateTime.Now)
                .DisposeWith(disposables);
            
            Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMinutes(10))
                .SelectMany(_ => Observable.FromAsync(ct => weatherService.Get().GetWeatherAsync(ct)))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(UpdateWeather, ex => logger.LogError(ex, ex.Message))
                .DisposeWith(disposables);
        });
    }

    private void UpdateWeather(WeatherInfo weather)
    {
        var icon = GetWeatherIconResource(weather.Weather, weather.IsDaylight);
        using var stream = AssetLoader.Open(new Uri(icon));

        WeatherIcon = new SvgImage
        {
            Source = SvgSource.LoadFromStream(stream)
        };
        
        Temperature = weather.Temperature;
    }
    
    private static string GetWeatherIconResource(WeatherCode code, bool isDaylight)
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

        string GetIconPath(string name) => $"avares://AlarmClock/Assets/Images/Weather/{name}";
    }
}