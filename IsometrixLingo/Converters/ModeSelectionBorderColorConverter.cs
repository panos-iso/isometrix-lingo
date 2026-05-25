using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

public class ModeSelectionBorderColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditMode currentMode && parameter is string targetMode)
        {
            if (targetMode == "Edit")
            {
                // Edit mode box: blue when selected, lighter blue when not
                return currentMode == EditMode.Edit 
                    ? Color.Parse("#1976D2")  // Dark blue when selected
                    : Color.Parse("#2196F3");  // Light blue when not selected
            }
            else if (targetMode == "Suggest")
            {
                // Suggest mode box: purple when selected, lighter purple when not
                return currentMode == EditMode.Suggest 
                    ? Color.Parse("#7B1FA2")  // Dark purple when selected
                    : Color.Parse("#9C27B0");  // Light purple when not selected
            }
        }

        return Color.Parse("#2196F3"); // Default to blue
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
