using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IsometrixLingo.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(string message) : this()
    {
        var messageText = this.FindControl<TextBlock>("MessageText");
        if (messageText != null)
        {
            messageText.Text = message;
        }
    }

    private void OnStayClicked(object? sender, RoutedEventArgs e)
    {
        Close(false); // Return false = stay on edit step
    }

    private void OnContinueClicked(object? sender, RoutedEventArgs e)
    {
        Close(true); // Return true = continue to export
    }
}
