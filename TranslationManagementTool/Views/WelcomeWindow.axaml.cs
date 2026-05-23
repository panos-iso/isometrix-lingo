using Avalonia.Controls;
using Avalonia.Interactivity;
using TranslationManagementTool.ViewModels;

namespace TranslationManagementTool.Views;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();
        DataContext = new WelcomeViewModel();
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WelcomeViewModel viewModel)
        {
            viewModel.SaveUsername();
            Close();
        }
    }
}
