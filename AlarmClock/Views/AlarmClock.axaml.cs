using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class AlarmClockView : ReactiveUserControl<AlarmClockViewModel>
{
    public AlarmClockView()
    {
        InitializeComponent();
    }
}