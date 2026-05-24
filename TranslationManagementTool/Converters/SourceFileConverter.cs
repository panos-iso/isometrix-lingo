using System;
using System.Globalization;
using Avalonia.Data.Converters;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.Converters;

public class SourceFileConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SourceFile source)
        {
            var type = source.Type == FileType.Json ? "JSON" : "RESX";
            return $"{source.Name} ({type})";
        }

        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
