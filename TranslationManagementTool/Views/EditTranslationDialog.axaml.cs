using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TranslationManagementTool.Views;

public partial class EditTranslationDialog : Window
{
    public EditTranslationDialog()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
