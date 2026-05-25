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
        if (values.Count < 1 || parameter is not string language)
        {
            return Brushes.Transparent;
        }

        var modifiedLanguages = values[0] as HashSet<string>;

        // Highlight if this language was modified
        // Using subtle orange/amber background with strong left border (like VS Code git changes)
        // Works well in both light and dark modes
        if (modifiedLanguages?.Contains(language) == true)
        {
            return new SolidColorBrush(Color.FromArgb(60, 255, 152, 0)); // Subtle orange background
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
