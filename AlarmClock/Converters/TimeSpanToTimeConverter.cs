using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AlarmClock.Converters;

public class TimeSpanToTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        
        return timeSpan.ToString(@"hh\:mm\:ss");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}