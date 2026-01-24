using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class ClockView : ReactiveUserControl<ClockViewModel>
{
    public ClockView()
    {
        InitializeComponent();
    }
}