using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter that returns a grey brush for modified keys, otherwise theme-appropriate foreground
/// </summary>
public class ConfirmationForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush GreyBrush = new(Color.Parse("#999999"));
    private static readonly SolidColorBrush WhiteBrush = new(Colors.White);
    private static readonly SolidColorBrush BlackBrush = new(Colors.Black);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isModified && isModified)
        {
            return GreyBrush;
        }

        // Return white for dark mode, black for light mode
        var theme = Application.Current?.ActualThemeVariant;
        return theme == ThemeVariant.Dark ? WhiteBrush : BlackBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
