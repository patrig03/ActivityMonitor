using System;
using Avalonia.Data.Converters;

namespace ActivityMonitor.Converters;

public class IsCategoryTwoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is int id && id == 2;

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
