using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using TranslationManagementTool.Converters;
using TranslationManagementTool.Helpers;
using TranslationManagementTool.Models;
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
            viewModel.OnEditTranslationRequested += OnEditTranslationRequested;
        }
    }

    private async void OnEditTranslationRequested(object? sender, TranslationKey translationKey)
    {
        if (DataContext is not MainWindowViewModel mainViewModel)
            return;

        var editViewModel = new EditTranslationViewModel(translationKey, mainViewModel.TranslationStore);
        var dialog = new EditTranslationDialog
        {
            DataContext = editViewModel
        };

        var result = await dialog.ShowDialog<bool>(this);

        if (result)
        {
            // Refresh UI to show updated values
            mainViewModel.TranslationStore.RefreshUI();

            // Update status message to show modified count
            var modifiedCount = mainViewModel.TranslationStore.GetModifiedKeys().Count;
            mainViewModel.HasModifiedKeys = modifiedCount > 0;

            if (modifiedCount > 0)
            {
                var currentStatus = mainViewModel.StatusMessage.Split('.')[0];
                mainViewModel.StatusMessage = $"{currentStatus}. {modifiedCount} key(s) modified.";
            }
        }
    }

    private void OnLanguagesChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // Remove existing language columns (keep Key, Source File, and Actions button)
        while (TranslationsGrid.Columns.Count > 3)
        {
            TranslationsGrid.Columns.RemoveAt(3);
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