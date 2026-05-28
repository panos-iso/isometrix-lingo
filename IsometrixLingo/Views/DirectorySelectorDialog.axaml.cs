using Avalonia.Controls;
using Avalonia.Interactivity;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Views;

public partial class DirectorySelectorDialog : Window
{
    public DirectorySelectorDialog()
    {
        InitializeComponent();
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DirectorySelectorViewModel viewModel && viewModel.HasSelection)
        {
            Close(true);
        }
        else
        {
            // Could show a message that at least one directory must be selected
            Close(false);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
