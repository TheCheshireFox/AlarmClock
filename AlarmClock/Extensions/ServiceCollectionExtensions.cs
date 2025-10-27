using System;
using System.Text.Json;
using AlarmClock.AlarmBuzzer;
using AlarmClock.Announcer;
using AlarmClock.Audio;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioSink;
using AlarmClock.BacklightController;
using AlarmClock.BacklightController.BrightnessPolicy;
using AlarmClock.Configuration;
using AlarmClock.DependencyInjection;
using AlarmClock.DisplayController;
using AlarmClock.Logger;
using AlarmClock.Radio;
using AlarmClock.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AlsBrightnessPolicy = AlarmClock.BacklightController.BrightnessPolicy.AlsBrightnessPolicy;
using SchedulerBrightnessPolicy = AlarmClock.BacklightController.BrightnessPolicy.SchedulerBrightnessPolicy;

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
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
        
        services.AddKeyedOptionServiceProvider<IAlarmBuzzer, BuzzerConfiguration, BuzzerType>(x => x.Type)
            .Add<SoundAlarmBuzzer>(BuzzerType.Sound)
            .Add<RadioAlarmBuzzer>(BuzzerType.Radio);
        
        services.AddKeyedOptionServiceProvider<IAnnouncer, AnnouncerConfiguration, AnnouncerType>(x => x.Type)
            .Add<PiperAnnouncer>(AnnouncerType.Piper)
            .Add<SilentAnnouncer>(AnnouncerType.Silent);
        
        services.AddKeyedOptionServiceProvider<IBrightnessPolicy, BacklightControlConfiguration, BacklightControlPolicy>(x => x.Policy)
            .Add<SchedulerBrightnessPolicy>(BacklightControlPolicy.Scheduled)
            .Add<AlsBrightnessPolicy>(BacklightControlPolicy.ALS)
            .Add<DummyBrightnessPolicy>(BacklightControlPolicy.None);
        
        services.AddKeyedOptionServiceProvider<IDisplayController, DisplayControllerConfiguration, DisplayControllerType>(x => x.Type)
            .Add<PwmDisplayController>(DisplayControllerType.PWM)
            .Add<DummyDisplayController>(DisplayControllerType.None);
        
        services.AddKeyedOptionServiceProvider<IWeatherProvider, WeatherConfiguration, WeatherProviderType>(x => x.Type)
            .Add<OpenWeatherProvider>(WeatherProviderType.OpenWeather)
            .Add<DummyWeatherProvider>(WeatherProviderType.None);
        
        services.AddSingleton<IAlarmService, AlarmService>();
        services.AddSingleton<IAudioDevice, AudioDevice>();
        services.AddSingleton<IBacklightController, BacklightController.BacklightController>();
        services.AddSingleton<IRadioPlayerFactory, RadioPlayerFactory>();
        services.AddSingleton<IRadioListProvider, RadioListProvider>();
        services.AddSingleton<IAudioSink, SoxAudioSink>();
        services.AddSingleton<IAudioDevice, AudioDevice>();
        services.AddSingleton<IStatusBus, StatusBus>();
        services.AddSingleton<IAlarmListProvider, AlarmListProvider>();

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

        services.AddSingleton<IJsonConfigManager>(_ => new JsonConfigManager(PathProvider.GetConfigPath(), new JsonSerializerOptions
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
    
    
    private static void ConfigureUsingPath<TOption>(this IServiceCollection services, IConfiguration configuration)
        where TOption : class
    {
        services.Configure<TOption>(configuration.GetSection(ConfigurationMetadataProvider.GetPath<TOption>()));
    }
    
    private static KeyedOptionServiceProviderBuilder<T, TKey> AddKeyedOptionServiceProvider<T, TOption, TKey>(this IServiceCollection serviceCollection, Func<TOption, TKey> keyGetter)
        where T : class
    {
        serviceCollection.AddSingleton<IKeyedOptionServiceProvider<T>>(sp => new KeyedOptionServiceProvider<T, TOption>(
            sp.GetRequiredService<IOptionsMonitor<TOption>>(),
            sp.GetRequiredService<IKeyedServiceProvider>(),
            opt => keyGetter(opt) ?? throw new InvalidOperationException($"Key for they type {typeof(TOption)} is null")));

        return new KeyedOptionServiceProviderBuilder<T, TKey>(serviceCollection);
    }
}