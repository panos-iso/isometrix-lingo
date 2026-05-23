using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using TranslationManagementTool.ViewModels;

namespace TranslationManagementTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.LanguagesChanged += OnLanguagesChanged;
        }
    }

    private void OnLanguagesChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // Remove existing language columns (keep Key and Source File)
        while (TranslationsGrid.Columns.Count > 2)
        {
            TranslationsGrid.Columns.RemoveAt(2);
        }

        // Add a column for each language
        foreach (var language in viewModel.Languages.OrderBy(l => l))
        {
            var column = new DataGridTextColumn
            {
                Header = language.ToUpperInvariant(),
                Width = new DataGridLength(1.5, DataGridLengthUnitType.Star),
                Binding = new Binding($"LanguageValues[{language}]")
            };
            TranslationsGrid.Columns.Add(column);
        }
    }
}