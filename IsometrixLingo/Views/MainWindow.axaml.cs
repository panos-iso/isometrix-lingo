using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using IsometrixLingo.Converters;
using IsometrixLingo.Helpers;
using IsometrixLingo.Models;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Views;

public partial class MainWindow : Window
{
    private static readonly LanguageValueConverter LanguageConverter = new();
    private static readonly TranslationValueConverter TranslationConverter = new();
    private static readonly ModifiedCellBackgroundConverter ModifiedBackgroundConverter = new();
    private static readonly ModifiedCellBorderConverter ModifiedBorderConverter = new();
    private static readonly ShowOriginalTooltipConverter ShowOriginalTooltipConverter = new();
    private static readonly RowToggleEnabledConverter RowToggleEnabledConverter = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;

        // Set up drag and drop for the entire drop zone area
        DragDrop.SetAllowDrop(DropZone, true);
        DragDrop.AddDragOverHandler(DropZone, DragOver);
        DragDrop.AddDropHandler(DropZone, Drop);
        DragDrop.AddDragEnterHandler(DropZone, DragEnter);
        DragDrop.AddDragLeaveHandler(DropZone, DragLeave);

        // Also set up on the overlay to catch all drag events across the entire area
        DragDrop.SetAllowDrop(DropOverlay, true);
        DragDrop.AddDragOverHandler(DropOverlay, DragOver);
        DragDrop.AddDropHandler(DropOverlay, Drop);
        DragDrop.AddDragEnterHandler(DropOverlay, DragEnter);
        DragDrop.AddDragLeaveHandler(DropOverlay, DragLeave);
    }

    private bool _isClosingConfirmed = false;

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && viewModel.HasUnsavedChanges && !_isClosingConfirmed)
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
                _isClosingConfirmed = true;
                Close();
            }
            else if (result == false)
            {
                // Discard and close
                _isClosingConfirmed = true;
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

            // Initialize language columns immediately
            OnLanguagesChanged(this, EventArgs.Empty);
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

        // Remove existing language columns and Actions (keep Key and Source File)
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
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 120,
                CellTemplate = new FuncDataTemplate<object>((_, _) =>
                {
                    // Create a border to apply background color and left border for highlighting
                    var border = new Border
                    {
                        Padding = new Avalonia.Thickness(5, 2),
                        BorderThickness = new Avalonia.Thickness(3, 0, 0, 0) // Left border
                    };

                    // Bind background to highlight modified cells
                    var backgroundBinding = new MultiBinding
                    {
                        Converter = ModifiedBackgroundConverter,
                        Bindings =
                        {
                            new Binding("ModifiedLanguages"), // HashSet that changes
                            new Binding(language) // Just a constant for the language parameter
                        },
                        ConverterParameter = language
                    };
                    border.Bind(Border.BackgroundProperty, backgroundBinding);

                    // Bind left border color to show modification indicator (like VS Code git gutter)
                    var borderBinding = new MultiBinding
                    {
                        Converter = ModifiedBorderConverter,
                        Bindings =
                        {
                            new Binding("ModifiedLanguages"),
                            new Binding(language)
                        },
                        ConverterParameter = language
                    };
                    border.Bind(Border.BorderBrushProperty, borderBinding);

                    var textBlock = new TextBlock
                    {
                        TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                        TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };

                    // Bind text to show either current or original value
                    var textBinding = new MultiBinding
                    {
                        Converter = TranslationConverter,
                        Bindings =
                        {
                            new Binding("LanguageValues") { Mode = BindingMode.OneWay }, // Dictionary that changes
                            new Binding("OriginalValues") { Mode = BindingMode.OneWay }, // Original values
                            new Binding("ShowOriginalValues") { Source = viewModel, Mode = BindingMode.OneWay }, // Global toggle from ViewModel
                            new Binding("ShowOriginalForThisRow") { Mode = BindingMode.OneWay } // Per-row toggle
                        },
                        ConverterParameter = language,
                        Mode = BindingMode.OneWay
                    };
                    textBlock.Bind(TextBlock.TextProperty, textBinding);

                    border.Child = textBlock;
                    return border;
                })
            };
            TranslationsGrid.Columns.Add(column);
        }

        // Add Actions column at the end
        var actionsColumn = new DataGridTemplateColumn
        {
            Header = "Actions",
            Width = DataGridLength.Auto,
            MinWidth = 100,
            CellTemplate = new FuncDataTemplate<object>((data, _) =>
            {
                var panel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 5,
                    Margin = new Avalonia.Thickness(5, 2)
                };

                // Show Original toggle button
                var toggleButton = new Button
                {
                    Content = "👁",
                    FontSize = 18,
                    Padding = new Avalonia.Thickness(8, 4),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                if (data is TranslationKey key)
                {
                    // Bind IsEnabled to IsModified AND NOT ShowOriginalValues
                    var enabledBinding = new MultiBinding
                    {
                        Converter = RowToggleEnabledConverter,
                        Bindings =
                        {
                            new Binding("IsModified") { Source = key },
                            new Binding("ShowOriginalValues") { Source = viewModel }
                        }
                    };
                    toggleButton.Bind(Button.IsEnabledProperty, enabledBinding);

                    // Bind tooltip to ShowOriginalForThisRow for context-aware text
                    var tooltipBinding = new Binding("ShowOriginalForThisRow")
                    {
                        Source = key,
                        Converter = ShowOriginalTooltipConverter
                    };
                    toggleButton.Bind(ToolTip.TipProperty, tooltipBinding);

                    // Bind Command to toggle ShowOriginalForThisRow
                    toggleButton.Click += (s, e) =>
                    {
                        key.ShowOriginalForThisRow = !key.ShowOriginalForThisRow;
                    };
                }

                // Edit button
                var editButton = new Button
                {
                    Content = "📝",
                    FontSize = 18,
                    Padding = new Avalonia.Thickness(8, 4),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Command = viewModel.EditTranslationCommand,
                    CommandParameter = data
                };
                ToolTip.SetTip(editButton, "Edit translation");

                panel.Children.Add(toggleButton);
                panel.Children.Add(editButton);
                return panel;
            })
        };
        TranslationsGrid.Columns.Add(actionsColumn);
    }

    private void OnGridDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (TranslationsGrid.SelectedItem is TranslationKey selectedKey &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.EditTranslationCommand.Execute(selectedKey);
        }
    }

    private void DragEnter(object? sender, DragEventArgs e)
    {
        // Always update the DropZone border, regardless of which element triggered the event
        DropZone.BorderBrush = new SolidColorBrush(Color.Parse("#1976D2"));
        DropZone.BorderThickness = new Thickness(4);
    }

    private void DragLeave(object? sender, DragEventArgs e)
    {
        // Always update the DropZone border, regardless of which element triggered the event
        DropZone.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
        DropZone.BorderThickness = new Thickness(3);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        // Check if files are being dragged
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        // Reset the DropZone border
        DropZone.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
        DropZone.BorderThickness = new Thickness(3);

        try
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.StatusMessage = "Processing dropped files...";

                if (e.DataTransfer.Formats.Contains(DataFormat.File))
                {
                    var files = e.DataTransfer.TryGetFiles();
                    if (files != null)
                    {
                        var fileList = files.ToList();
                        viewModel.StatusMessage = $"Found {fileList.Count} file(s) in drop";
                        if (fileList.Count > 0)
                        {
                            await viewModel.ImportDroppedFiles(fileList);
                        }
                        else
                        {
                            viewModel.StatusMessage = "No files in drop";
                        }
                    }
                    else
                    {
                        viewModel.StatusMessage = "TryGetFiles returned null";
                    }
                }
                else
                {
                    var formats = string.Join(", ", e.DataTransfer.Formats);
                    viewModel.StatusMessage = $"Drop does not contain files. Formats: {formats}";
                }
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.StatusMessage = $"Drop error: {ex.Message}";
            }
        }
    }
}