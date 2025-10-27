using System;
using AlarmClock.DependencyInjection;
using AlarmClock.Design;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AlarmClock.Components;

public enum DisplayPage
{
    Clock,
    Alarm,
    Settings
}

public partial class DisplayArea : UserControl
{
    [Inject]
    private IAlarmService AlarmService { get; set; } = AppService.GetDefault<IAlarmService>();
    
    public DisplayArea()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        AlarmService.Changed += s =>
        {
            if (s == AlarmState.WentOff)
                ShowPage(DisplayPage.Alarm);
        };
    }

    public void ShowPage(DisplayPage page)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            foreach (var p in Pages.Children)
            {
                p.IsVisible = Enum.Parse<DisplayPage>(p.Tag?.ToString()!) == page;
            }
        });
    }
}