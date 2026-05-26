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
using Avalonia.Markup.Xaml.MarkupExtensions;
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
    private static readonly EditButtonTooltipConverter EditButtonTooltipConverter = new();
    private static readonly ConfirmationForegroundConverter ConfirmationForegroundConverter = new();

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

        var editViewModel = new EditTranslationViewModel(
            translationKey, 
            mainViewModel.TranslationStore, 
            mainViewModel.CurrentMode,
            mainViewModel.Username);
        var dialog = new EditTranslationDialog
        {
            DataContext = editViewModel
        };

        var result = await dialog.ShowDialog<bool>(this);

        if (result)
        {
            // Mark as having unsaved changes (for both Edit and Suggest modes)
            mainViewModel.HasUnsavedChanges = true;
            
            // Update status message to show modified count
            var modifiedCount = mainViewModel.TranslationStore.GetModifiedKeys().Count;

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

        // Remove existing columns after Key and Source File (keep first 2)
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
                MaxWidth = 300,
                CellTemplate = new FuncDataTemplate<object>((data, _) =>
                {
                    // Create a border to apply background color and left border for highlighting
                    var border = new Border
                    {
                        Padding = new Avalonia.Thickness(5, 4),
                        BorderThickness = new Avalonia.Thickness(3, 0, 0, 0) // Left border
                    };

                    // Bind background to highlight modified cells and cells with suggestions
                    var backgroundBinding = new MultiBinding
                    {
                        Converter = ModifiedBackgroundConverter,
                        Bindings =
                        {
                            new Binding("ModifiedLanguages"), // HashSet that changes
                            new Binding("."), // The TranslationKey itself for suggestion check
                            new Binding("SuggestedValues"), // Explicit binding to trigger refresh when suggestions change
                            new Binding(language) // Just a constant for the language parameter
                        },
                        ConverterParameter = language
                    };
                    border.Bind(Border.BackgroundProperty, backgroundBinding);

                    // Bind left border color to show modification (orange) and suggestion (purple) indicators
                    var borderBinding = new MultiBinding
                    {
                        Converter = ModifiedBorderConverter,
                        Bindings =
                        {
                            new Binding("ModifiedLanguages"),
                            new Binding("."), // The TranslationKey itself for suggestion check
                            new Binding("SuggestedValues"), // Explicit binding to trigger refresh when suggestions change
                            new Binding(language)
                        },
                        ConverterParameter = language
                    };
                    border.Bind(Border.BorderBrushProperty, borderBinding);

                    // Create a Grid with two columns: text content and buttons
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star)); // Text content
                    grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Buttons

                    // Create a StackPanel to hold actual value and suggestion
                    var stackPanel = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Vertical,
                        Spacing = 2
                    };
                    Grid.SetColumn(stackPanel, 0);

                    // Actual value TextBlock
                    var actualValueTextBlock = new TextBlock
                    {
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
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
                    actualValueTextBlock.Bind(TextBlock.TextProperty, textBinding);

                    stackPanel.Children.Add(actualValueTextBlock);

                    // Suggestion TextBlock (only visible if there's a suggestion)
                    if (data is TranslationKey key)
                    {
                        var suggestionTextBlock = new TextBlock
                        {
                            FontSize = 12,
                            FontWeight = FontWeight.Bold,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 2, 0, 0),
                            [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("SuggestionTextBrush")
                        };

                        // Bind suggestion text
                        var suggestionTextBinding = new MultiBinding
                        {
                            Converter = new SuggestionTextConverter(),
                            Bindings =
                            {
                                new Binding(".") { Source = key },
                                new Binding("SuggestedValues") { Source = key } // Explicit binding to trigger refresh
                            },
                            ConverterParameter = language
                        };
                        suggestionTextBlock.Bind(TextBlock.TextProperty, suggestionTextBinding);

                        // Bind visibility - only show if there's a suggestion
                        var suggestionVisibilityBinding = new MultiBinding
                        {
                            Converter = new HasSuggestionConverter(),
                            Bindings =
                            {
                                new Binding(".") { Source = key },
                                new Binding("SuggestedValues") { Source = key } // Explicit binding to trigger refresh
                            },
                            ConverterParameter = language
                        };
                        suggestionTextBlock.Bind(TextBlock.IsVisibleProperty, suggestionVisibilityBinding);

                        stackPanel.Children.Add(suggestionTextBlock);

                        // Accept/Reject buttons (only visible in Edit Mode and when there's a suggestion)
                        var buttonsPanel = new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            Spacing = 3,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                            Margin = new Avalonia.Thickness(5, 0, 0, 0)
                        };
                        Grid.SetColumn(buttonsPanel, 1);

                        // Accept button (✓)
                        var acceptButton = new Button
                        {
                            Content = "✓",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.Parse("#4CAF50")), // Green
                            Background = Brushes.Transparent,
                            BorderBrush = new SolidColorBrush(Color.Parse("#4CAF50")),
                            BorderThickness = new Avalonia.Thickness(1),
                            Padding = new Avalonia.Thickness(6, 2),
                            CornerRadius = new Avalonia.CornerRadius(3),
                            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            Command = viewModel.AcceptSuggestionCommand,
                            CommandParameter = (key, language)
                        };
                        ToolTip.SetTip(acceptButton, "Accept suggestion");

                        // Reject button (✗)
                        var rejectButton = new Button
                        {
                            Content = "✗",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.Parse("#F44336")), // Red
                            Background = Brushes.Transparent,
                            BorderBrush = new SolidColorBrush(Color.Parse("#F44336")),
                            BorderThickness = new Avalonia.Thickness(1),
                            Padding = new Avalonia.Thickness(6, 2),
                            CornerRadius = new Avalonia.CornerRadius(3),
                            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            Command = viewModel.RejectSuggestionCommand,
                            CommandParameter = (key, language)
                        };
                        ToolTip.SetTip(rejectButton, "Reject suggestion");

                        // Bind visibility based on EditMode and HasSuggestion
                        // Only show buttons in Edit Mode AND when there's a suggestion for this language
                        var buttonsVisibilityBinding = new MultiBinding
                        {
                            Converter = new EditModeAndHasSuggestionConverter(),
                            Bindings =
                            {
                                new Binding("CurrentMode") { Source = viewModel },
                                new Binding(".") { Source = key },
                                new Binding("SuggestedValues") { Source = key } // Explicit binding to trigger refresh
                            },
                            ConverterParameter = language
                        };
                        buttonsPanel.Bind(StackPanel.IsVisibleProperty, buttonsVisibilityBinding);

                        buttonsPanel.Children.Add(acceptButton);
                        buttonsPanel.Children.Add(rejectButton);

                        grid.Children.Add(stackPanel);
                        grid.Children.Add(buttonsPanel);
                    }

                    border.Child = grid;
                    return border;
                })
            };
            TranslationsGrid.Columns.Add(column);
        }

        // Add Last Confirmed column before Actions
        var confirmedColumn = new DataGridTemplateColumn
        {
            Header = "Last Confirmed By",
            Width = new DataGridLength(1.2, DataGridLengthUnitType.Star),
            MinWidth = 180,
            MaxWidth = 250,
            CellTemplate = new FuncDataTemplate<object>((data, _) =>
            {
                if (data is not TranslationKey key || key.ConfirmedBy == null)
                    return new TextBlock();

                var panel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Spacing = 4,
                    Margin = new Avalonia.Thickness(5, 4),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // Username text - don't set Foreground at all, let theme handle it
                var usernameText = new TextBlock
                {
                    Text = key.ConfirmedBy.Username,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // Date badge
                var dateBorder = new Border
                {
                    Background = Brushes.DarkSlateGray,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Avalonia.Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(3),
                    Padding = new Avalonia.Thickness(6, 2),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var dateText = new TextBlock
                {
                    Text = key.ConfirmedBy.Timestamp.ToString("MMM dd, yyyy"),
                    FontSize = 11,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Brushes.White
                };

                dateBorder.Child = dateText;
                panel.Children.Add(usernameText);
                panel.Children.Add(dateBorder);

                return panel;
            })
        };
        TranslationsGrid.Columns.Add(confirmedColumn);

        // Add Actions column at the end
        var actionsColumn = new DataGridTemplateColumn
        {
            Header = "Actions",
            Width = DataGridLength.Auto,
            MinWidth = 100,
            MaxWidth = 120,
            CellTemplate = new FuncDataTemplate<object>((data, _) =>
            {
                var panel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 5,
                    Margin = new Avalonia.Thickness(5, 2)
                };

                // Show Original toggle button (only visible in Edit mode)
                var toggleButton = new Button
                {
                    Content = "👁",
                    FontSize = 18,
                    Padding = new Avalonia.Thickness(8, 4),
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // Bind IsVisible to CurrentMode (only show in Edit mode)
                var visibilityBinding = new Binding("CurrentMode")
                {
                    Source = viewModel,
                    Converter = new IsEditModeConverter()
                };
                toggleButton.Bind(Button.IsVisibleProperty, visibilityBinding);

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
                
                // Bind tooltip to CurrentMode for context-aware text
                var editTooltipBinding = new Binding("CurrentMode")
                {
                    Source = viewModel,
                    Converter = EditButtonTooltipConverter
                };
                editButton.Bind(ToolTip.TipProperty, editTooltipBinding);

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

    private void OnEditModeBoxTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectModeCommand.Execute(EditMode.Edit);
        }
    }

    private void OnSuggestModeBoxTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectModeCommand.Execute(EditMode.Suggest);
        }
    }
}