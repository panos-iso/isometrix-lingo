using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to highlight modified translation cells
/// </summary>
public class ModifiedCellBackgroundConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not TranslationKey translationKey || values[1] is not string language)
        {
            return Brushes.Transparent;
        }

        // Highlight if this language was modified
        if (translationKey.IsLanguageModified(language))
        {
            return new SolidColorBrush(Color.FromArgb(40, 255, 235, 59)); // Light yellow highlight
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
