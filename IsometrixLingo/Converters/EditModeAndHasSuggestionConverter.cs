using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to check if buttons should be visible: Edit Mode AND has suggestion for language
/// </summary>
public class EditModeAndHasSuggestionConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || parameter is not string language)
        {
            return false;
        }

        var mode = values[0] as EditMode? ?? EditMode.Edit;
        var key = values[1] as TranslationKey;

        // Show buttons only in Edit Mode AND when there's a suggestion
        return mode == EditMode.Edit && key?.HasSuggestion(language) == true;
    }
}
