using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to check if a TranslationKey has a suggestion for a specific language
/// </summary>
public class HasSuggestionConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is TranslationKey key && parameter is string language)
        {
            return key.HasSuggestion(language);
        }

        return false;
    }
}
