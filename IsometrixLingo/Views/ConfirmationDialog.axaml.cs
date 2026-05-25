using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IsometrixLingo.Views;

public partial class ConfirmationDialog : Window
{
    private bool? _result = null;

    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(string message, string? title = null, string? header = null) : this()
    {
        if (!string.IsNullOrEmpty(title))
        {
            Title = title;
        }
        
        var headerText = this.FindControl<TextBlock>("HeaderText");
        if (headerText != null && !string.IsNullOrEmpty(header))
        {
            headerText.Text = header;
        }
        
        var messageText = this.FindControl<TextBlock>("MessageText");
        if (messageText != null)
        {
            messageText.Text = message;
        }
    }

    private void OnStayClicked(object? sender, RoutedEventArgs e)
    {
        _result = false;
        Close(_result);
    }

    private void OnContinueClicked(object? sender, RoutedEventArgs e)
    {
        _result = true;
        Close(_result);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        // If closing without a result set (X button, ESC, etc.), default to false
        if (_result == null && !e.Cancel)
        {
            _result = false;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // Ensure we always return false if no button was clicked
        if (_result == null)
        {
            _result = false;
        }
    }
}
