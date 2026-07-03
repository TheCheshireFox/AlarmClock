using System;
using AlarmClock.Announcer;
using AlarmClock.Audio.AudioManager;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using AlarmClock.Configuration.Toml;
using AlarmClock.DependencyInjection;
using AlarmClock.Display.BacklightController;
using AlarmClock.Display.BacklightController.BrightnessPolicy;
using AlarmClock.Display.DisplayController;
using AlarmClock.Gpio;
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
using Tomlyn;
using AlsBrightnessPolicy = AlarmClock.Display.BacklightController.BrightnessPolicy.AlsBrightnessPolicy;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using SchedulerBrightnessPolicy = AlarmClock.Display.BacklightController.BrightnessPolicy.SchedulerBrightnessPolicy;

namespace AlarmClock.Extensions;

public class KeyedOptionServiceProviderBuilder<TInterface, TKey>(IServiceCollection services)
    where TInterface : class
{
    public KeyedOptionServiceProviderBuilder<TInterface, TKey> Add<TImplementation>(TKey key)
        where TImplementation : class, TInterface
    {
        services.AddKeyedSingleton<TInterface, TImplementation>(key);
        return this;
    }
}

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddViews()
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

        public IServiceCollection AddServices()
        {
#if NOLGPIO
            services.AddSingleton<ILGpio, NopLGpio>();
#else
            services.AddSingleton<ILGpio, LGpio>();
#endif
            
            services.AddSingleton<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
        
            services.AddKeyedOptionService<IAlarmBuzzer, BuzzerConfiguration, BuzzerType>(x => x.Type)
                .Add<SoundAlarmBuzzer>(BuzzerType.Sound)
                .Add<RadioAlarmBuzzer>(BuzzerType.Radio)
                .Add<GpioAlarmBuzzer>(BuzzerType.Gpio);

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
            services.AddSingleton(CreateFallbackBuzzer);

            services.AddSingleton<IBacklightControllerConfig, BacklightControllerConfig>();
            services.AddSingleton<IBacklightSchedulerConfig, BacklightSchedulerConfig>();
            services.AddSingleton<IPwmDisplayControllerConfig, PwmDisplayControllerConfig>();
            services.AddSingleton<IOpenWeatherProviderConfig, OpenWeatherProviderConfig>();
            services.AddSingleton<IRadioAlarmBuzzerConfig, RadioAlarmBuzzerConfig>();
            services.AddSingleton<ISoundAlarmBuzzerConfig, SoundAlarmBuzzerConfig>();
            services.AddSingleton<IPiperAnnouncerConfig, PiperAnnouncerConfig>();
            services.AddSingleton<IGpioBuzzerConfig, GpioAlarmBuzzerConfig>();

            return services;
        }

        public IServiceCollection AddConfiguration(TomlSerializerOptions options)
        {
            var builder = new ConfigurationBuilder()
                .AddTomlFile(src =>
                {
                    src.Path = PathProvider.GetConfigPath();
                    src.Options = options;
                    src.ReloadOnChange = true;
                    src.Optional = false;
                    src.ResolveFileProvider();
                });
            
            var configuration = builder.Build();
        
            services.ConfigureUsingPath<AlarmConfiguration>(configuration);
            services.ConfigureUsingPath<AnnouncerConfiguration>(configuration);
            services.ConfigureUsingPath<BacklightControlConfiguration>(configuration);
            services.ConfigureUsingPath<BuzzerConfiguration>(configuration);
            services.ConfigureUsingPath<DisplayControllerConfiguration>(configuration);
            services.ConfigureUsingPath<WeatherConfiguration>(configuration);
            services.ConfigureUsingPath<RadioConfiguration>(configuration);

            services.AddSingleton<IConfigManager>(_ => new TomlConfigManager(PathProvider.GetConfigPath(), options));
        
            return services;
        }

        public IServiceCollection AddServiceLogging()
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole(options => options.FormatterName = TemplateConsoleFormatter.FormatterName)
                    .AddConsoleFormatter<TemplateConsoleFormatter, TemplateConsoleFormatterOptions>()
                    .SetMinimumLevel(LogLevel.Debug);
            });

            return services;
        }

        private IServiceCollection AddView<TModel, TView>()
            where TModel : class
            where TView : class, IViewFor<TModel>
        {
            return services
                .AddSingleton<TModel>()
                .AddSingleton<TView>()
                .AddSingleton<IViewFor<TModel>>(sp => sp.GetRequiredService<TView>());
        }

        private void ConfigureUsingPath<TOption>(IConfiguration configuration)
            where TOption : class
        {
            services.Configure<TOption>(configuration.GetSection(ConfigurationMetadataProvider.GetPath<TOption>()));
        }

        private KeyedOptionServiceProviderBuilder<T, TKey> AddKeyedOptionService<T, TOption, TKey>(Func<TOption, TKey> keyGetter)
            where T : class
        {
            services.AddSingleton<IService<T>>(sp => new KeyedOptionService<T, TOption>(
                sp.GetRequiredService<IOptionsMonitor<TOption>>(),
                sp.GetRequiredService<IKeyedServiceProvider>(),
                opt => keyGetter(opt) ?? throw new InvalidOperationException($"Key for they type {typeof(TOption)} is null")));

            return new KeyedOptionServiceProviderBuilder<T, TKey>(services);
        }
    }

    private static IAlarmBuzzer CreateFallbackBuzzer(IServiceProvider serviceProvider)
    {
        var fallback = serviceProvider.GetRequiredKeyedService<IAlarmBuzzer>(BuzzerType.Gpio);
        return ActivatorUtilities.CreateInstance<FallbackAlarmBuzzer>(serviceProvider, fallback);
    }
}
