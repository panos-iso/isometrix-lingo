using System;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Converters;

/// <summary>
/// Converter to get the minimal display path for a SourceFile from the ViewModel
/// Parameter should be the MainWindowViewModel instance
/// </summary>
public class SourceFileMinimalPathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SourceFile source && parameter is MainWindowViewModel viewModel)
        {
            return viewModel.GetMinimalDisplayPath(source);
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
