using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to show left border indicator for modified cells (orange) and cells with suggestions (purple)
/// Priority: Modified (orange) > Suggestion (purple)
/// </summary>
public class ModifiedCellBorderConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || parameter is not string language)
        {
            return Brushes.Transparent;
        }

        var modifiedLanguages = values[0] as HashSet<string>;
        var key = values[1] as TranslationKey;

        // Priority 1: Show orange border if this language was modified
        if (modifiedLanguages?.Contains(language) == true)
        {
            return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Solid orange border
        }

        // Priority 2: Show purple border if there's a suggestion for this language
        if (key?.HasSuggestion(language) == true)
        {
            return new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Solid purple border (#9C27B0)
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
