using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that truncates a directory path if it's too long
/// </summary>
public class TruncatedDirectoryPathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string directoryPath || string.IsNullOrEmpty(directoryPath))
            return null;

        const int maxLength = 60;
        if (directoryPath.Length <= maxLength)
            return directoryPath;

        // Truncate in the middle
        var parts = directoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2)
            return directoryPath;

        // Show first part, "...", and last 2 parts
        var firstPart = parts[0];
        var lastParts = string.Join("/", parts.Skip(parts.Length - 2));
        var truncated = $"{firstPart}/.../{ lastParts}";

        // If still too long, just use last part
        if (truncated.Length > maxLength)
        {
            truncated = $".../{parts[parts.Length - 1]}";
        }

        return truncated;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
