using Avalonia.Controls;
using Avalonia.Interactivity;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Views;

public partial class AddKeyDialog : Window
{
    public AddKeyDialog()
    {
        InitializeComponent();
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddKeyViewModel viewModel && viewModel.IsValid())
        {
            Close(true);
        }
        else
        {
            // Show validation error (could be improved with proper validation UI)
            var errorDialog = new Window
            {
                Title = "Validation Error",
                Width = 300,
                Height = 150,
                Content = new TextBlock
                {
                    Text = "Please fill in all required fields (Key Name and Source File).",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(20)
                },
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            errorDialog.ShowDialog(this);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
