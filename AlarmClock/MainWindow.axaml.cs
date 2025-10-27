using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.BacklightController;
using AlarmClock.Components;
using AlarmClock.DependencyInjection;
using AlarmClock.Design;
using AlarmClock.Radio;
using AlarmClock.Utility;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace AlarmClock;

public enum MenuElement
{
    Clock,
    Alarm,
    DisplayOff,
    Settings,
    Radio
}

public partial class MainWindow : Window
{
    private static readonly TimeSpan _menuTimeout = TimeSpan.FromSeconds(30);
    private readonly WakeShield _wakeShield = new();
    private readonly Subject<Unit> _taps = new();

    private IRadioPlayer _radioPlayer = null!;
    private bool _radioOn;

    [Inject]
    private IAlarmService AlarmService { get; set; } = AppService.GetDefault<IAlarmService>();
    
    [Inject]
    private IBacklightController BacklightController { get; set; } = AppService.GetDefault<IBacklightController>();

    [Inject]
    private IRadioPlayerFactory RadioPlayerFactory { get; set; } = AppService.GetDefault<IRadioPlayerFactory>();
    
    [Inject]
    private IStatusBus StatusBus { get; set; } = AppService.GetDefault<IStatusBus>();
    
    [Inject]
    private ILogger<MainWindow> Logger { get; set; } = AppService.GetDefault<ILogger<MainWindow>>();
        
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        LeftNavBar.MenuSelected += OnMenuSelected;
        RightNavBar.MenuSelected += OnMenuSelected;

        _taps.Do(_ => SetMenuVisible(true))
            .Throttle(_menuTimeout)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ =>
            {
                ResetPage();
                SetMenuVisible(false);
            });

        AddHandler(PointerPressedEvent, (_, _) => _taps.OnNext(Unit.Default));
        SetMenuVisible(false);
            
        _wakeShield.Attach(this);
        _wakeShield.Wake += async () => await BacklightController.EnableAsync(true, ApplicationCancellation.Token);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _radioPlayer = RadioPlayerFactory.Create(AudioPriority.Low);
    }

    private async void OnMenuSelected(object? sender, MenuElement menu)
    {
        try
        {
            switch (menu)
            {
                case MenuElement.Clock:
                    DisplayArea.ShowPage(DisplayPage.Clock);
                    break;
                case MenuElement.Alarm:
                    DisplayArea.ShowPage(DisplayPage.Alarm);
                    break;
                case MenuElement.Settings:
                    DisplayArea.ShowPage(DisplayPage.Settings);
                    break;
                case MenuElement.DisplayOff:
                {
                    if (await BacklightController.EnableAsync(false, ApplicationCancellation.Token))
                        _wakeShield.Activate();
                    break;
                }
                case MenuElement.Radio:
                {
                    if (_radioOn)
                    {
                        await _radioPlayer.StopAsync(ApplicationCancellation.Token);
                        await StatusBus.PublishAsync(new StatusEvent(Status.RadioOff), ApplicationCancellation.Token);
                    }
                    else
                    {
                        await _radioPlayer.PlayAsync("radioparadise", ApplicationCancellation.Token);
                        await StatusBus.PublishAsync(new StatusEvent(Status.RadioOn), ApplicationCancellation.Token);
                    }

                    _radioOn = !_radioOn;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnMenuSelected");
        }
    }
    
    private void SetMenuVisible(bool value) => LeftNavBar.IsVisible = RightNavBar.IsVisible = value;
    private void ResetPage() => DisplayArea.ShowPage(AlarmService.State == AlarmState.WentOff ? DisplayPage.Alarm : DisplayPage.Clock);
}