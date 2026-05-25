using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns a red/rose background color when a translation key has missing translations
/// </summary>
public class MissingTranslationBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasMissingTranslations && hasMissingTranslations)
        {
            return new SolidColorBrush(Color.FromArgb(40, 220, 53, 69)); // Light red/rose background (semi-transparent)
        }

        return Brushes.Transparent; // Transparent background for normal rows
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
