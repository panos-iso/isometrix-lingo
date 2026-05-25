using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to get formatted suggestion text for display
/// Returns: "→ suggested_value (username, timestamp)" or empty string if no suggestion
/// </summary>
public class SuggestionTextConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is TranslationKey key && parameter is string language)
        {
            if (key.SuggestedValues.TryGetValue(language, out var suggestion))
            {
                var date = suggestion.Timestamp.ToString("MMM dd", CultureInfo.InvariantCulture);
                return $"→ {suggestion.Value} ({suggestion.Username}, {date})";
            }
        }

        return string.Empty;
    }
}
