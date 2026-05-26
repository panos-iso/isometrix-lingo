using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns a grey brush for modified keys, otherwise null to use theme default
/// </summary>
public class ConfirmationForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush GreyBrush = new(Color.Parse("#999999"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isModified && isModified)
        {
            return GreyBrush;
        }

        // Return null to let the theme's default foreground apply
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
