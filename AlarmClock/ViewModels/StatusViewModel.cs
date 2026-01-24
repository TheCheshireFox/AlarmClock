using System.Reactive.Concurrency;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public interface IStatusNotifier
{
    void SetAlarm(bool enabled);
    void SetRadio(bool enabled);
}

public class StatusViewModel: ReactiveObject, IStatusNotifier
{
    public bool IsAlarmEnabled
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsRadioEnabled
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void SetAlarm(bool enabled)
    {
        RxApp.MainThreadScheduler.Schedule(() => IsAlarmEnabled = enabled);
    }

    public void SetRadio(bool enabled)
    {
        RxApp.MainThreadScheduler.Schedule(() => IsRadioEnabled = enabled);
    }
}