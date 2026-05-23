using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using TranslationManagementTool.Converters;
using TranslationManagementTool.Helpers;
using TranslationManagementTool.ViewModels;

namespace TranslationManagementTool.Views;

public partial class MainWindow : Window
{
    private static readonly LanguageValueConverter LanguageConverter = new();

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
            var languageName = LanguageHelper.GetLanguageName(language);
            var column = new DataGridTemplateColumn
            {
                Header = languageName,
                Width = DataGridLength.Auto,
                MinWidth = 120,
                MaxWidth = 300,
                CellTemplate = new FuncDataTemplate<object>((_, _) =>
                {
                    var textBlock = new TextBlock
                    {
                        TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                        TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                        Margin = new Avalonia.Thickness(5, 2)
                    };
                    var binding = new Binding("LanguageValues")
                    {
                        Converter = LanguageConverter,
                        ConverterParameter = language
                    };
                    textBlock.Bind(TextBlock.TextProperty, binding);
                    return textBlock;
                })
            };
            TranslationsGrid.Columns.Add(column);
        }
    }
}