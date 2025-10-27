using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AlarmClock.BacklightController;
using AlarmClock.DependencyInjection;
using AlarmClock.Extensions;
using AlarmClock.Utility;
using AlarmClock.Weather;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlarmClock
{
    public class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            ServiceCollection services = [];
            services.AddServices();
            services.AddServiceLogging();
            services.AddConfiguration();

            _serviceProvider =  services.BuildServiceProvider();
        }
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += (_, _) => _serviceProvider.Dispose();
                
                var scope = _serviceProvider.CreateScope();

                desktop.MainWindow = ActivatorUtilities.CreateInstance<MainWindow>(scope.ServiceProvider);
                desktop.MainWindow.Closed += (_, _) => scope.Dispose();
                desktop.MainWindow.Loaded += async (_, _) => await InitializeServicesAsync();
                
                InjectTree(scope.ServiceProvider,  desktop.MainWindow);
            }

            base.OnFrameworkInitializationCompleted();
        }

        // dependencies will be accessible in OnLoad
        private static void InjectTree(IServiceProvider serviceProvider, Control control)
        {
            foreach (var property in control.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                if (property.GetCustomAttribute<InjectAttribute>() == null)
                    continue;
                
                property.SetValue(control, serviceProvider.GetRequiredService(property.PropertyType));
            }

            foreach (var child in control.GetLogicalDescendants().OfType<Control>())
                InjectTree(serviceProvider, child);
        }

        private async Task InitializeServicesAsync()
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Initializing services...");
            
            var alarmService = _serviceProvider.GetRequiredService<IAlarmService>();
            await alarmService.InitializeAsync(ApplicationCancellation.Token);
            
            var backlightController =  _serviceProvider.GetRequiredService<IBacklightController>();
            await backlightController.StartAsync(ApplicationCancellation.Token);
            
            var weatherProvider = _serviceProvider.GetRequiredService<IKeyedOptionServiceProvider<IWeatherProvider>>();
            await weatherProvider.Get().InitializeAsync(ApplicationCancellation.Token);
            
            logger.LogInformation("Service initialized");
        }
    }
}