using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IsometrixLingo.Views;

public partial class ErrorDetailsDialog : Window
{
    public ErrorDetailsDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
