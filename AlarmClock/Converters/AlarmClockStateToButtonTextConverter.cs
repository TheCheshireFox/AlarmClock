using System;
using System.Globalization;
using AlarmClock.ViewModels;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AlarmClock.Converters;

public class AlarmClockStateToButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AlarmClockState state)
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);

        return state switch
        {
            AlarmClockState.Armed => "STOP",
            AlarmClockState.Disarmed => "SET",
            AlarmClockState.Ringing => "RESET",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}