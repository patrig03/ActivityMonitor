using System;
using Avalonia.Data.Converters;

namespace ActivityMonitor.Converters
{
    public class IsCategoryTwoConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int id && id == 2)
                return true;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            throw new NotImplementedException();
    }
}