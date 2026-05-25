using System;
using System.Globalization;
using Avalonia.Data.Converters;
using IsometrixLingo.Models;

namespace IsometrixLingo.Converters;

public class EditDialogTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EditMode mode)
        {
            return mode == EditMode.Edit 
                ? "Edit Translation"
                : "Suggest Translation Updates";
        }

        return "Edit Translation";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
