using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TranslationManagementTool.Converters;

public class LanguageValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<string, string> languageValues && parameter is string languageCode)
        {
            return languageValues.TryGetValue(languageCode, out var translationValue)
                ? translationValue
                : string.Empty;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
