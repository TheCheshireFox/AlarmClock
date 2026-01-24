using System.Reactive;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Configuration;
using AlarmClock.Display.BacklightController;
using AlarmClock.Radio;
using AlarmClock.Utility;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class NavBarViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, IRoutableViewModel> GoClock { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoAlarm { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoSettings { get; }
    public ReactiveCommand<Unit, bool> DisplayOff { get; }
    public ReactiveCommand<Unit, Unit> ToggleRadio { get; }
    
    public bool MenuVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public NavBarViewModel(INavigationHost navigationHost, IBacklightController backlightController, IRadioPlayerFactory radioPlayerFactory, IStatusNotifier statusNotifier, IOptionsMonitor<BuzzerConfiguration> options)
    {
        var radioPlayer = radioPlayerFactory.Create(AudioPriority.Low);

        GoClock = ReactiveCommand.CreateFromObservable(() => navigationHost.NavigateTo<ClockViewModel>());
        GoAlarm = ReactiveCommand.CreateFromObservable(() => navigationHost.NavigateTo<AlarmClockViewModel>());
        GoSettings = ReactiveCommand.CreateFromObservable(() => navigationHost.NavigateTo<SettingsViewModel>());
        DisplayOff = ReactiveCommand.CreateFromTask(ct => backlightController.EnableAsync(false, ct));
        ToggleRadio = ReactiveCommand.CreateFromTask(async ct =>
        {
            var radioName = string.IsNullOrEmpty(options.CurrentValue.Radio.Name)
                ? "radioparadise"
                : options.CurrentValue.Radio.Name;
            
            await (radioPlayer.IsPlaying
                ? radioPlayer.StopAsync(ct)
                : radioPlayer.PlayAsync(radioName, ct));
        
            statusNotifier.SetRadio(radioPlayer.IsPlaying);
        });
    }
}