using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that looks up minimal display path from a dictionary
/// Parameter should be the dictionary of minimal paths
/// </summary>
public class SourceFileMinimalPathLookupConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SourceFile source && parameter is Dictionary<SourceFile, string?> minimalPaths)
        {
            if (minimalPaths.TryGetValue(source, out var path))
            {
                return path;
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
