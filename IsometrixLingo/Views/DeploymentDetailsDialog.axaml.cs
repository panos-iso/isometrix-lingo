using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IsometrixLingo.Views;

public partial class DeploymentDetailsDialog : Window
{
    public DeploymentDetailsDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
