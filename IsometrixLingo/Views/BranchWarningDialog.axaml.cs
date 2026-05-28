using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IsometrixLingo.Views;

public partial class BranchWarningDialog : Window
{
    public BranchWarningDialog()
    {
        InitializeComponent();
    }

    private void OnProceedClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
