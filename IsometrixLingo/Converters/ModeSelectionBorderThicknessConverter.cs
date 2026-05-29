using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

public class ModeSelectionBorderThicknessConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditMode currentMode && parameter is string targetMode)
        {
            if ((targetMode == "Edit" && currentMode == EditMode.Edit) ||
                (targetMode == "Suggest" && currentMode == EditMode.Suggest) ||
                (targetMode == "Deployment" && currentMode == EditMode.Deployment))
            {
                // Selected: thick bold border
                return new Thickness(4);
            }
            else
            {
                // Not selected: thinner border
                return new Thickness(2);
            }
        }

        return new Thickness(2); // Default
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
