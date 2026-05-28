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
        // Always allow import - files may exist in parent directory even if no subdirectories selected
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
