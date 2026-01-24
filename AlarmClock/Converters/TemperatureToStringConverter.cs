using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AlarmClock.Converters;

public class TemperatureToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double temperature)
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);

        return $"{temperature:F1}°C";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}