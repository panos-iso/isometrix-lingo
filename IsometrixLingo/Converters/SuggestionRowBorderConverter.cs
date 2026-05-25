using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace IsometrixLingo.Converters;

public class SuggestionRowBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasSuggestions && hasSuggestions)
        {
            return new Thickness(4, 0, 0, 0); // Thick left border
        }

        return new Thickness(0); // No border
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
