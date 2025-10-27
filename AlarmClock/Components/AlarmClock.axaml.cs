using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.DependencyInjection;
using AlarmClock.Design;
using AlarmClock.Extensions;
using AlarmClock.Utility;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AlarmClock.Components;

public partial class AlarmClock : UserControl
{
    private const string RestartCaption = "RESTART";
    private const string SetCaption = "SET";
    private const string StopCaption = "STOP";

    [Inject]
    private IAlarmService AlarmService { get; set; } = AppService.GetDefault<IAlarmService>();
    
    [Inject]
    private IStatusBus StatusBus { get; set; } = AppService.GetDefault<IStatusBus>();
    
    public AlarmClock()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        AlarmService.Changed += s =>
        {
            switch (s)
            {
                case AlarmState.Started:
                    OnStart();
                    break;
                case AlarmState.Stopped:
                    OnStop();
                    break;
                case AlarmState.WentOff:
                    OnWentOff();
                    break;
            }
        };
        AlarmService.Ticked += OnTick;

        var alarm = await AlarmService.GetAlarmAsync(ApplicationCancellation.Token);
        TimePicker.Time = alarm.Time;
        
        if (alarm.Enabled)
            OnStart();
        else
            OnStop();
    }

    private async void OnStart()
    {
        await StatusBus.PublishAsync(new StatusEvent(Status.AlarmOff), ApplicationCancellation.Token);
        
        Dispatcher.UIThread.Invoke(() =>
        {
            ControlButtonText.Text = StopCaption;
            CountdownText.IsVisible = true;
            TimePicker.IsVisible = false;
        });
    }

    private async void OnWentOff()
    {
        await StatusBus.PublishAsync(new StatusEvent(Status.AlarmOn), ApplicationCancellation.Token);
        
        Dispatcher.UIThread.Invoke(() =>
        {
            ControlButtonText.Text = RestartCaption;
        });
    }

    private async void OnStop()
    {
        await StatusBus.PublishAsync(new StatusEvent(Status.AlarmOff), ApplicationCancellation.Token);
        
        Dispatcher.UIThread.Invoke(() =>
        {
            ControlButtonText.Text = SetCaption;
            CountdownText.IsVisible = false;
            TimePicker.IsVisible = true;
        });
    }

    private void OnTick(TimeSpan tick)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            CountdownText.Text = tick.ToString(@"hh\:mm\:ss");
        });
    }
    
    private async void ControlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var caption = ControlButtonText.Text;

            switch (caption)
            {
                case SetCaption:
                    await AlarmService.StartAsync(TimePicker.Time, ApplicationCancellation.Token);
                    break;
                case StopCaption:
                    await AlarmService.StopAsync(ApplicationCancellation.Token);
                    break;
                case RestartCaption:
                    await AlarmService.RestartAsync(ApplicationCancellation.Token);
                    break;
                default:
                    throw new Exception($"Invalid control caption: {caption}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}