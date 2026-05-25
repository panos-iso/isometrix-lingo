using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to show "Show Filters" or "Hide Filters" based on ShowFilters property
/// </summary>
public class ShowFiltersTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showFilters)
        {
            return showFilters ? "Hide Filters" : "Show Filters";
        }

        return "Show Filters";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
