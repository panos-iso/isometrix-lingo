using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to highlight modified translation cells and cells with suggestions
/// Priority: Modified (yellow) > Suggestion (light blue/purple)
/// </summary>
public class ModifiedCellBackgroundConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || parameter is not string language)
        {
            return Brushes.Transparent;
        }

        var modifiedLanguages = values[0] as HashSet<string>;
        var key = values[1] as TranslationKey;

        // Priority 1: Highlight if this language was modified
        // Using vibrant yellow background to clearly distinguish from red (missing translations)
        if (modifiedLanguages?.Contains(language) == true)
        {
            return new SolidColorBrush(Color.FromArgb(85, 255, 235, 59)); // Vibrant yellow background
        }

        // Priority 2: Light background if there's a suggestion for this language
        if (key?.HasSuggestion(language) == true)
        {
            return new SolidColorBrush(Color.FromArgb(40, 156, 39, 176)); // Light purple tint
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
