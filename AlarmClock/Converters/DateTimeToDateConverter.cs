using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AlarmClock.Converters;

public class DateTimeToDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        
        return dateTime.ToString("ddd dd/MM");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}