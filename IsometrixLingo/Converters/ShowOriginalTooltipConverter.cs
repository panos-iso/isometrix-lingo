using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to show appropriate tooltip for the show original toggle button
/// </summary>
public class ShowOriginalTooltipConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showingOriginal)
        {
            return showingOriginal ? "Show updated value" : "Show original value";
        }

        return "Show original value";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
