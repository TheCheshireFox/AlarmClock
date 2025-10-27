using System;
using Avalonia.Controls;

namespace AlarmClock.Components;

public partial class TimePicker : UserControl
{
    public TimeSpan Time
    {
        get => new(HoursPicker.SelectedItem, MinutesPicker.SelectedItem, 0);
        set
        {
            HoursPicker.SelectedItem = value.Hours;
            MinutesPicker.SelectedItem = value.Minutes;
        }
    }
    
    public TimePicker()
    {
        InitializeComponent();
    }
}