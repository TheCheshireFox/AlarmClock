using System.Threading.Tasks;
using AlarmClock.DependencyInjection;
using AlarmClock.Design;
using AlarmClock.Utility;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AlarmClock.Components;

public partial class Header : UserControl
{
    [Inject]
    private IStatusBus StatusBus { get; set; } = AppService.GetDefault<IStatusBus>();
    
    public Header()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _ = Task.Run(SubscribeStatusAsync);
    }

    private async Task SubscribeStatusAsync()
    {
        await foreach (var env in StatusBus.SubscribeAsync(ApplicationCancellation.Token))
        {
            switch (env.Status)
            {
                case Status.AlarmOn:
                    Dispatcher.UIThread.Invoke(() => AlarmStatus.IsVisible = true);
                    break;
                case Status.AlarmOff:
                    Dispatcher.UIThread.Invoke(() => AlarmStatus.IsVisible = false);
                    break;
                case Status.RadioOn:
                    Dispatcher.UIThread.Invoke(() => RadioStatus.IsVisible = true);
                    break;
                case Status.RadioOff:
                    Dispatcher.UIThread.Invoke(() => RadioStatus.IsVisible = false);
                    break;
            }
        }
    }
}