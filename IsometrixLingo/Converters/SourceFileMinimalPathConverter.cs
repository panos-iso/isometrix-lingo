using System;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to get the minimal display path for a SourceFile from the ViewModel
/// Requires the DataContext to be MainWindowViewModel
/// </summary>
public class SourceFileMinimalPathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SourceFile source)
        {
            // Try to get the minimal path from the current DataContext (MainWindowViewModel)
            // This will be set by the grid's DataContext
            if (parameter is ViewModels.MainWindowViewModel viewModel)
            {
                return viewModel.GetMinimalDisplayPath(source);
            }
            
            // Fallback: show directory path if available
            return source.DirectoryPath;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
