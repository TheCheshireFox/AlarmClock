using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Display.BacklightController;
using AlarmClock.Extensions;
using AlarmClock.Shared;
using AlarmClock.ViewModels;
using AlarmClock.Weather;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AlarmClock
{
    public class App : Application
    {
        public static ServiceProvider Services { get; private set; } = null!;

        public App()
        {
            ServiceCollection services = [];
            services.AddServices();
            services.AddViews();
            services.AddServiceLogging();
            services.AddConfiguration();

            Services = services.BuildServiceProvider();
        }
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var cts = new CancellationTokenSource();
                
                desktop.Exit += (_, _) =>
                {
                    cts.Cancel();
                    Services.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                };

                desktop.MainWindow = Services.GetRequiredService<MainWindow>();
                desktop.MainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();

                // force run on a thread pool to avoid UI deadlock
                Task.Run(() => InitializeServicesAsync(cts.Token), cts.Token).GetAwaiter().GetResult();
                InitializeDisplayArea();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void InitializeDisplayArea()
        {
            var screen = Services.GetRequiredService<IScreen>();
            var viewModelFactory = Services.GetRequiredService<IViewModelFactory>();

            screen.Router.NavigateAndReset
                .Execute(viewModelFactory.Create<ClockViewModel>())
                .Subscribe();
        }
        
        private static async Task InitializeServicesAsync(CancellationToken cancellationToken)
        {
            var logger = Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Initializing services...");
            
            var alarmService = Services.GetRequiredService<IAlarmService>();
            await alarmService.InitializeAsync(cancellationToken);
            
            var backlightController =  Services.GetRequiredService<IBacklightController>();
            await backlightController.StartAsync(cancellationToken);
            
            var weatherProvider = Services.GetRequiredService<IService<IWeatherProvider>>();
            await weatherProvider.Get().InitializeAsync(cancellationToken);
            
            logger.LogInformation("Service initialized");
        }
    }
}