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
        if (values.Count < 2 || values[0] is not TranslationKey translationKey || values[1] is not bool showOriginal)
        {
            return string.Empty;
        }

        var language = parameter as string;
        if (string.IsNullOrEmpty(language))
        {
            return string.Empty;
        }

        // If showing original and we have an original value, show it
        if (showOriginal && translationKey.OriginalValues.TryGetValue(language, out var originalValue))
        {
            return originalValue;
        }

        // Otherwise show current value
        if (translationKey.LanguageValues.TryGetValue(language, out var currentValue))
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
