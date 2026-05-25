using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that enables row-level toggle button only when:
/// - Row is modified (IsModified = true)
/// - Global ShowOriginalValues filter is OFF (ShowOriginalValues = false)
/// </summary>
public class RowToggleEnabledConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
            return false;

        var isModified = values[0] as bool? ?? false;
        var showOriginalValues = values[1] as bool? ?? false;

        // Enable only if modified AND global toggle is OFF
        return isModified && !showOriginalValues;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
