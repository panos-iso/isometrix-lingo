using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

public class SuggestionRowBorderBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasSuggestions && hasSuggestions)
        {
            return new SolidColorBrush(Color.Parse("#9C27B0")); // Purple border for suggestions
        }

        return Brushes.Transparent; // No border
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
