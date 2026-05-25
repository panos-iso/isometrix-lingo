using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Views;

public partial class EditTranslationDialog : Window
{
    public EditTranslationDialog()
    {
        InitializeComponent();
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditTranslationViewModel viewModel)
        {
            Close(true);
            return;
        }
        
        // Check if there are any pending suggestions in Edit mode
        if (viewModel.CurrentMode == Models.EditMode.Edit)
        {
            var pendingSuggestions = viewModel.LanguageValues
                .Where(lv => lv.HasSuggestion)
                .ToList();
            
            if (pendingSuggestions.Count > 0)
            {
                var result = await ShowPendingSuggestionsWarning(pendingSuggestions.Count);
                if (!result)
                {
                    // User cancelled - don't save
                    return;
                }
            }
        }
        
        // Proceed with save
        viewModel.SaveCommand.Execute(null);
        Close(true);
    }
    
    private async Task<bool> ShowPendingSuggestionsWarning(int count)
    {
        var language = count == 1 ? "language" : "languages";
        var message = $"There {(count == 1 ? "is" : "are")} {count} pending suggestion{(count == 1 ? "" : "s")} that will be discarded if you save now.\n\n" +
                     "You can accept or reject suggestions using the ✓ and ✗ buttons, or continue to save and discard them.\n\n" +
                     "Do you want to save and discard the pending suggestions?";
        
        var dialog = new Window
        {
            Title = "Pending Suggestions",
            Width = 450,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };
        
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };
        
        var saveButton = new Button { Content = "Save & Discard", Width = 120 };
        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        
        bool? result = null;
        
        saveButton.Click += (s, args) => { result = true; dialog.Close(); };
        cancelButton.Click += (s, args) => { result = false; dialog.Close(); };
        
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;
        
        await dialog.ShowDialog(this);
        
        return result == true;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
