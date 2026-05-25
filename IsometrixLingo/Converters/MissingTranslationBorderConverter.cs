using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns a red border brush when a translation key has missing translations
/// </summary>
public class MissingTranslationBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasMissingTranslations && hasMissingTranslations)
        {
            return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Bootstrap danger red
        }

        return null; // No border
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
