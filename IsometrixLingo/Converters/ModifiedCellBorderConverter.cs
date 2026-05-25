using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to show left border indicator for modified cells (like VS Code git gutter)
/// </summary>
public class ModifiedCellBorderConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 1 || parameter is not string language)
        {
            return Brushes.Transparent;
        }

        var modifiedLanguages = values[0] as HashSet<string>;

        // Show left border if this language was modified
        if (modifiedLanguages?.Contains(language) == true)
        {
            return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Solid orange border
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
