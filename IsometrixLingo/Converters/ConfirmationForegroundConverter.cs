using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns a grey brush for modified keys, otherwise default foreground
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

        return AvaloniaProperty.UnsetValue; // Use system default foreground
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
