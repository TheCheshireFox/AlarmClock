using System;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class TimePickerViewModel : ReactiveObject
{
    public NumberPickerViewModel HoursPicker { get; } = new() { Min = 0, Max = 23 };

    public NumberPickerViewModel MinutesPicker { get; } = new() { Min = 0, Max = 60 };

    public bool IsVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public TimeSpan Time
    {
        get => new(HoursPicker.SelectedItem, MinutesPicker.SelectedItem, 0);
        set
        {
            HoursPicker.SelectedItem = value.Hours;
            MinutesPicker.SelectedItem = value.Minutes;
        }
    }
}