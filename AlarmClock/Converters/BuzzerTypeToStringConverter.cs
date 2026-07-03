using System;
using System.Globalization;
using AlarmClock.Configuration;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AlarmClock.Converters;

public class BuzzerTypeToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BuzzerType type)
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);

        return type switch
        {
            BuzzerType.Radio => "RADIO",
            BuzzerType.Sound => "SOUND",
            BuzzerType.Gpio => "BUZZER",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}