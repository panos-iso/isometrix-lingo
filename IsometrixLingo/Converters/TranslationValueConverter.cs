using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to show either current or original translation value
/// Parameter should be the language code
/// </summary>
public class TranslationValueConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3 || parameter is not string language)
        {
            return string.Empty;
        }

        var languageValues = values[0] as Dictionary<string, string>;
        var originalValues = values[1] as Dictionary<string, string>;
        var showOriginal = values[2] is bool show && show;

        if (string.IsNullOrEmpty(language))
        {
            return string.Empty;
        }

        // If showing original and we have an original value, show it
        if (showOriginal && originalValues?.TryGetValue(language, out var originalValue) == true)
        {
            return originalValue;
        }

        // Otherwise show current value
        if (languageValues?.TryGetValue(language, out var currentValue) == true)
        {
            return currentValue;
        }

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
