using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class TimePickerView : ReactiveUserControl<TimePickerViewModel>
{
    public TimePickerView()
    {
        InitializeComponent();
    }
}