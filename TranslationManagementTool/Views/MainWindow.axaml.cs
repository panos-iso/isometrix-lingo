using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && viewModel.HasUnsavedChanges)
        {
            // Cancel the close temporarily to show dialog
            e.Cancel = true;

            var dialog = new Window
            {
                Title = "Unsaved Changes",
                Width = 400,
                Height = 200,
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
                Text = "You have unsaved changes. What would you like to do?",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 10
            };

            var saveButton = new Button { Content = "Save & Exit", Width = 100 };
            var discardButton = new Button { Content = "Discard & Exit", Width = 100 };
            var cancelButton = new Button { Content = "Cancel", Width = 100 };

            bool? result = null;

            saveButton.Click += (s, args) => { result = true; dialog.Close(); };
            discardButton.Click += (s, args) => { result = false; dialog.Close(); };
            cancelButton.Click += (s, args) => { result = null; dialog.Close(); };

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(discardButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            await dialog.ShowDialog(this);

            if (result == true)
            {
                // Save and close
                viewModel.SaveProgressCommand.Execute(null);
                Close();
            }
            else if (result == false)
            {
                // Discard and close
                Close();
            }
            // If result is null, user cancelled - do nothing
        }
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

            if (modifiedCount > 0)
            {
                var currentStatus = mainViewModel.StatusMessage.Split('.')[0];
                mainViewModel.StatusMessage = $"{currentStatus}. {modifiedCount} key(s) modified.";
            }
            
            // Auto-save after editing
            mainViewModel.SaveProgressCommand.Execute(null);
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