using System;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Extensions;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public enum AlarmClockState
{
    Armed,
    Disarmed,
    Ringing
}

public class AlarmClockViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
{
    private readonly IAlarmService _alarmService;
    private readonly IStatusNotifier _statusNotifier;

    public string? UrlPathSegment { get; } = nameof(AlarmClockViewModel);
    public IScreen HostScreen { get; }
    public ViewModelActivator Activator { get; }
    
    public TimePickerViewModel TimePicker { get; }

    public ReactiveCommand<Unit, Unit> ChangeState { get; }
    
    public AlarmClockState State
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public TimeSpan Countdown
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool CountdownVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public AlarmClockViewModel(IScreen screen, IAlarmService alarmService, TimePickerViewModel timePickerViewModel, IStatusNotifier statusNotifier)
    {
        _alarmService = alarmService;
        TimePicker = timePickerViewModel;
        _statusNotifier = statusNotifier;

        HostScreen = screen;
        Activator = new ViewModelActivator();

        ChangeState = ReactiveCommand.CreateFromTask(ChangeStateAsync);
        
        this.WhenActivated(disposables =>
        {
            _alarmService.Ticked
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(x => Countdown = x)
                .DisposeWith(disposables);

            _alarmService.StateChanged
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case AlarmState.Started:
                            CountdownVisible = true;
                            TimePicker.IsVisible = false;
                            State = AlarmClockState.Armed;
                            break;
                        case AlarmState.Stopped:
                            CountdownVisible = false;
                            TimePicker.IsVisible = true;
                            State = AlarmClockState.Disarmed;
                            break;
                        case AlarmState.WentOff:
                            CountdownVisible = true;
                            TimePicker.IsVisible = false;
                            State = AlarmClockState.Ringing;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(state), state, null);
                    }
                }).DisposeWith(disposables);
            
            Observable
                .FromAsync(_alarmService.GetAlarmAsync)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(Initialize)
                .DisposeWith(disposables);
        });
    }

    private void Initialize(AlarmSettings alarmSettings)
    {
        State = alarmSettings.Enabled ? AlarmClockState.Armed : AlarmClockState.Disarmed;
        CountdownVisible = alarmSettings.Enabled;
        TimePicker.IsVisible = !alarmSettings.Enabled;
        TimePicker.Time = alarmSettings.Time;
    }
    
    private async Task ChangeStateAsync(CancellationToken cancellationToken)
    {
        switch (State)
        {
            case AlarmClockState.Armed:
                await _alarmService.StopAsync(cancellationToken);
                State = AlarmClockState.Disarmed;
                _statusNotifier.SetAlarm(false);
                break;
            case AlarmClockState.Disarmed:
                await _alarmService.StartAsync(TimePicker.Time, cancellationToken);
                State = AlarmClockState.Armed;
                _statusNotifier.SetAlarm(true);
                break;
            case AlarmClockState.Ringing:
                await _alarmService.RestartAsync(cancellationToken);
                State = AlarmClockState.Ringing;
                _statusNotifier.SetAlarm(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(State), State, null);
        }
    }
}