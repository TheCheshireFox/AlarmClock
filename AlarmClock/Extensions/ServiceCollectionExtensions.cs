using System;
using System.Text.Json;
using AlarmClock.Announcer;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioManager;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using AlarmClock.DependencyInjection;
using AlarmClock.Display.BacklightController;
using AlarmClock.Display.BacklightController.BrightnessPolicy;
using AlarmClock.Display.DisplayController;
using AlarmClock.ListProviders;
using AlarmClock.Logging;
using AlarmClock.Network;
using AlarmClock.Radio;
using AlarmClock.ServiceConfiguration;
using AlarmClock.Shared;
using AlarmClock.ViewModels;
using AlarmClock.Views;
using AlarmClock.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using AlsBrightnessPolicy = AlarmClock.Display.BacklightController.BrightnessPolicy.AlsBrightnessPolicy;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using SchedulerBrightnessPolicy = AlarmClock.Display.BacklightController.BrightnessPolicy.SchedulerBrightnessPolicy;

namespace AlarmClock.Extensions;

public class KeyedOptionServiceProviderBuilder<TInterface, TKey>
    where TInterface : class
{
    private readonly IServiceCollection _services;

    public KeyedOptionServiceProviderBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public KeyedOptionServiceProviderBuilder<TInterface, TKey> Add<TImplementation>(TKey key)
        where TImplementation : class, TInterface
    {
        _services.AddKeyedSingleton<TInterface, TImplementation>(key);
        return this;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddView<ClockViewModel, ClockView>();
        services.AddView<AlarmClockViewModel, AlarmClockView>();
        services.AddView<NumberPickerViewModel, NumberPickerView>();
        services.AddView<TimePickerViewModel, TimePickerView>();
        services.AddView<DisplayAreaViewModel, DisplayAreaView>();
        services.AddView<StatusViewModel, HeaderView>();
        services.AddView<StatusViewModel, FooterView>();
        services.AddView<SettingsViewModel, SettingsView>();
        services.AddView<WiFiSettingsViewModel, WiFiSettingsView>();
        services.AddView<MainWindowViewModel, MainWindow>();
        services.AddView<NavBarViewModel, LeftNavBarView>();
        services.AddView<NavBarViewModel, RightNavBarView>();

        services.AddSingleton<INavigationHost>(sp => sp.GetRequiredService<DisplayAreaViewModel>());
        services.AddSingleton<IScreen>(sp => sp.GetRequiredService<DisplayAreaViewModel>());

        services.AddSingleton<IStatusNotifier>(sp => sp.GetRequiredService<StatusViewModel>());
        
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        
        return services;
    }
    
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
        
        services.AddKeyedOptionService<IAlarmBuzzer, BuzzerConfiguration, BuzzerType>(x => x.Type)
            .Add<SoundAlarmBuzzer>(BuzzerType.Sound)
            .Add<RadioAlarmBuzzer>(BuzzerType.Radio);
        
        services.AddKeyedOptionService<IAnnouncer, AnnouncerConfiguration, AnnouncerType>(x => x.Type)
            .Add<PiperAnnouncer>(AnnouncerType.Piper)
            .Add<SilentAnnouncer>(AnnouncerType.Silent);
        
        services.AddKeyedOptionService<IBrightnessPolicy, BacklightControlConfiguration, BacklightControlPolicy>(x => x.Policy)
            .Add<SchedulerBrightnessPolicy>(BacklightControlPolicy.Scheduled)
            .Add<AlsBrightnessPolicy>(BacklightControlPolicy.ALS)
            .Add<DummyBrightnessPolicy>(BacklightControlPolicy.None);
        
        services.AddKeyedOptionService<IDisplayController, DisplayControllerConfiguration, DisplayControllerType>(x => x.Type)
            .Add<PwmDisplayController>(DisplayControllerType.PWM)
            .Add<DummyDisplayController>(DisplayControllerType.None);
        
        services.AddKeyedOptionService<IWeatherProvider, WeatherConfiguration, WeatherProviderType>(x => x.Type)
            .Add<OpenWeatherProvider>(WeatherProviderType.OpenWeather)
            .Add<DummyWeatherProvider>(WeatherProviderType.None);
        
        services.AddSingleton<IAlarmService, AlarmService>();
        services.AddSingleton<IAudioManager, AudioManager>();
        services.AddSingleton<IBacklightController, BacklightController>();
        services.AddSingleton<IRadioPlayerFactory, RadioPlayerFactory>();
        services.AddSingleton<IRadioListProvider, RadioListProvider>();
        services.AddSingleton<IAudioSink, SoxAudioSink>();
        services.AddSingleton<IAlarmListProvider, AlarmListProvider>();
        services.AddSingleton<IWiFiManager, WpaSupplicantWifiManager>();

        services.AddSingleton<IBacklightControllerConfig, BacklightControllerConfig>();
        services.AddSingleton<IBacklightSchedulerConfig, BacklightSchedulerConfig>();
        services.AddSingleton<IOpenWeatherProviderConfig, OpenWeatherProviderConfig>();
        services.AddSingleton<IRadioAlarmBuzzerConfig, RadioAlarmBuzzerConfig>();
        services.AddSingleton<ISoundAlarmBuzzerConfig, SoundAlarmBuzzerConfig>();
        services.AddSingleton<IPiperAnnouncerConfig, PiperAnnouncerConfig>();

        return services;
    }

    public static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
            .AddSnakeCaseJsonFile(PathProvider.GetConfigPath(), optional: false, reloadOnChange: true);
        
        var configuration = builder.Build();
        
        services.ConfigureUsingPath<AlarmConfiguration>(configuration);
        services.ConfigureUsingPath<AnnouncerConfiguration>(configuration);
        services.ConfigureUsingPath<BacklightControlConfiguration>(configuration);
        services.ConfigureUsingPath<BuzzerConfiguration>(configuration);
        services.ConfigureUsingPath<DisplayControllerConfiguration>(configuration);
        services.ConfigureUsingPath<WeatherConfiguration>(configuration);

        services.AddSingleton<IConfigManager>(_ => new ConfigManager(PathProvider.GetConfigPath(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        }));
        
        return services;
    }
    
    public static IServiceCollection AddServiceLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole(options => options.FormatterName = TemplateConsoleFormatter.FormatterName)
                .AddConsoleFormatter<TemplateConsoleFormatter, TemplateConsoleFormatterOptions>()
                .SetMinimumLevel(LogLevel.Debug);
        });

        return services;
    }

    public static IServiceCollection AddSplat(this IServiceCollection services)
    {
        services.UseMicrosoftDependencyResolver();
        Locator.CurrentMutable.InitializeSplat();
        
        RxSchedulers.MainThreadScheduler = AvaloniaScheduler.Instance;
        
        return services;
    }

    private static IServiceCollection AddView<TModel, TView>(this IServiceCollection services)
        where TModel : class
        where TView : class, IViewFor<TModel>
    {
        return services
            .AddSingleton<TModel>()
            .AddSingleton<TView>()
            .AddSingleton<IViewFor<TModel>>(sp => sp.GetRequiredService<TView>());
    }

    private static void ConfigureUsingPath<TOption>(this IServiceCollection services, IConfiguration configuration)
        where TOption : class
    {
        services.Configure<TOption>(configuration.GetSection(ConfigurationMetadataProvider.GetPath<TOption>()));
    }
    
    private static KeyedOptionServiceProviderBuilder<T, TKey> AddKeyedOptionService<T, TOption, TKey>(this IServiceCollection serviceCollection, Func<TOption, TKey> keyGetter)
        where T : class
    {
        serviceCollection.AddSingleton<IService<T>>(sp => new KeyedOptionService<T, TOption>(
            sp.GetRequiredService<IOptionsMonitor<TOption>>(),
            sp.GetRequiredService<IKeyedServiceProvider>(),
            opt => keyGetter(opt) ?? throw new InvalidOperationException($"Key for they type {typeof(TOption)} is null")));

        return new KeyedOptionServiceProviderBuilder<T, TKey>(serviceCollection);
    }
}
