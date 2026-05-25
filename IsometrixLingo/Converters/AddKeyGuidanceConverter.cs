using System;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

public class AddKeyGuidanceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditMode mode && parameter is string parameterType)
        {
            if (parameterType == "header")
            {
                return mode == EditMode.Edit 
                    ? "Translations (Optional)"
                    : "Suggested Translations (Optional)";
            }
            else if (parameterType == "subtext")
            {
                return mode == EditMode.Edit 
                    ? "You can add translations now or edit them later"
                    : "You can suggest translations now or add suggestions later";
            }
        }

        return "Translations (Optional)";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
