using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Converters;

/// <summary>
/// MultiValue converter that gets minimal display path from ViewModel
/// Values: [0] = SourceFile, [1] = MainWindowViewModel
/// </summary>
public class SourceFileMinimalPathMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null && values.Count >= 2)
        {
            if (values[0] is SourceFile source && values[1] is MainWindowViewModel viewModel)
            {
                return viewModel.GetMinimalDisplayPath(source);
            }
        }

        return null;
    }
}
