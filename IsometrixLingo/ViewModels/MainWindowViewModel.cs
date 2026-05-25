using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IsometrixLingo.Models;
using IsometrixLingo.Services;
using IsometrixLingo.Views;

namespace IsometrixLingo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TranslationStore _translationStore;
    private readonly JsonTranslationFileReader _jsonReader;
    private readonly ResxTranslationFileReader _resxReader;
    private readonly JsonTranslationFileWriter _jsonWriter;
    private readonly ResxTranslationFileWriter _resxWriter;
    private readonly ProgressService _progressService;
    private readonly UserSettingsService _settingsService;
    private string _lastExportFolder = string.Empty;
    private string _lastExportFileName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click Import to load translation files.";

    [ObservableProperty]
    private string _username = "User";

    [ObservableProperty]
    private ObservableCollection<SourceFile?> _availableSourceFiles = new();

    [ObservableProperty]
    private SourceFile? _selectedSourceFile;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    private bool _hasKeys;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private ObservableCollection<string> _importedFileNames = new();

    // Workflow state properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowImportStep), nameof(ShowEditStep), nameof(ShowExportStep), 
                               nameof(Step1Background), nameof(Step2Background), nameof(Step3Background),
                               nameof(Step1Status), nameof(Step2Status), nameof(Step3Status))]
    private WorkflowStep _currentStep = WorkflowStep.Import;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step1Background), nameof(Step1Foreground), nameof(Step1Status))]
    private StepStatus _importStepStatus = StepStatus.InProgress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step2Background), nameof(Step2Foreground), nameof(Step2Status))]
    private StepStatus _editStepStatus = StepStatus.NotStarted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step3Background), nameof(Step3Foreground), nameof(Step3Status), nameof(StartOverButtonText))]
    private StepStatus _exportStepStatus = StepStatus.NotStarted;

    public bool ShowImportStep => CurrentStep == WorkflowStep.Import;
    public bool ShowEditStep => CurrentStep == WorkflowStep.Edit;
    public bool ShowExportStep => CurrentStep == WorkflowStep.Export;

    public string StartOverButtonText => ExportStepStatus == StepStatus.Completed ? "Start New Session" : "Start Over";

    public SolidColorBrush Step1Background => ImportStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step2Background => EditStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step3Background => ExportStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step1Foreground => ImportStepStatus switch
    {
        StepStatus.NotStarted => new SolidColorBrush(Colors.White),
        _ => new SolidColorBrush(Colors.White)
    };

    public SolidColorBrush Step2Foreground => EditStepStatus switch
    {
        StepStatus.NotStarted => new SolidColorBrush(Colors.White),
        _ => new SolidColorBrush(Colors.White)
    };

    public SolidColorBrush Step3Foreground => ExportStepStatus switch
    {
        StepStatus.NotStarted => new SolidColorBrush(Colors.White),
        _ => new SolidColorBrush(Colors.White)
    };

    public string Step1Status => ImportStepStatus switch
    {
        StepStatus.Completed => "✓ Complete",
        StepStatus.InProgress => "In Progress",
        _ => "Not Started"
    };

    public string Step2Status => EditStepStatus switch
    {
        StepStatus.Completed => "✓ Complete",
        StepStatus.InProgress => "In Progress",
        _ => "Not Started"
    };

    public string Step3Status => ExportStepStatus switch
    {
        StepStatus.Completed => "✓ Complete",
        StepStatus.InProgress => "In Progress",
        _ => "Not Started"
    };

    public bool CanImport => !HasKeys;

    public event EventHandler? LanguagesChanged;

    public MainWindowViewModel()
    {
        _translationStore = new TranslationStore();
        _jsonReader = new JsonTranslationFileReader();
        _resxReader = new ResxTranslationFileReader();
        _jsonWriter = new JsonTranslationFileWriter();
        _resxWriter = new ResxTranslationFileWriter();
        _progressService = new ProgressService();
        _settingsService = new UserSettingsService();

        // Load username from settings
        LoadUserSettings();

        // Add "All Files" as first option (null value)
        AvailableSourceFiles.Add(null);

        // Subscribe to unsaved changes
        _translationStore.UnsavedChangesChanged += (s, e) =>
        {
            HasUnsavedChanges = _translationStore.HasUnsavedChanges;
        };

        // Auto-load saved progress on startup
        LoadProgress();
    }

    public TranslationStore TranslationStore => _translationStore;

    public IReadOnlyCollection<string> Languages => _translationStore.Languages;

    [RelayCommand]
    private async Task ImportFiles(Window window)
    {
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Translation Files",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Translation Files")
                {
                    Patterns = new[] { "*.json", "*.resx" }
                }
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        var translationFiles = new List<TranslationFile>();
        ImportedFileNames.Clear();

        foreach (var file in files)
        {
            try
            {
                var filePath = file.Path.LocalPath;
                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath).ToLower();

                TranslationFile translationFile = extension switch
                {
                    ".json" => _jsonReader.ReadFile(filePath),
                    ".resx" => _resxReader.ReadFile(filePath),
                    _ => throw new NotSupportedException($"Unsupported file type: {extension}")
                };

                translationFiles.Add(translationFile);
                ImportedFileNames.Add(fileName);
            }
            catch
            {
                // Skip invalid files
            }
        }

        // Group files by base name and file type, then consolidate
        var groupedFiles = translationFiles
            .GroupBy(tf => (ExtractBaseFileName(tf.FilePath, tf.FileType), tf.FileType))
            .ToList();

        foreach (var group in groupedFiles)
        {
            var consolidated = group.Key.FileType == FileType.Json
                ? _jsonReader.ConsolidateKeys(group.ToList())
                : _resxReader.ConsolidateKeys(group.ToList());

            _translationStore.AddTranslations(consolidated);

            // Extract and store the template from the first file in the group
            if (group.Any())
            {
                var firstFile = group.First();
                if (group.Key.FileType == FileType.Resx)
                {
                    var template = _resxReader.ExtractTemplate(firstFile.FilePath);
                    _translationStore.SetResxTemplate(group.Key.Item1, template);
                }
                else if (group.Key.FileType == FileType.Json)
                {
                    var template = _jsonReader.ExtractTemplate(firstFile.FilePath);
                    _translationStore.SetJsonTemplate(group.Key.Item1, template);
                }
            }
        }

        UpdateFileFilters();
        StatusMessage = $"Imported {files.Count} file(s). Review the imported files below and click 'Confirm & Continue' to proceed.";
        HasKeys = _translationStore.GetAllKeys().Count > 0;
        ImportStepStatus = StepStatus.InProgress;
        LanguagesChanged?.Invoke(this, EventArgs.Empty);

        // Auto-save imported progress
        SaveProgress();
    }

    [RelayCommand]
    private async Task AddKey(Window window)
    {
        var addKeyViewModel = new AddKeyViewModel(
            _translationStore.SourceFiles,
            _translationStore.Languages,
            SelectedSourceFile  // Pass current filter selection as default
        );

        var dialog = new AddKeyDialog
        {
            DataContext = addKeyViewModel
        };

        var result = await dialog.ShowDialog<bool>(window);

        if (result)
        {
            var newKey = addKeyViewModel.CreateTranslationKey();
            _translationStore.AddKey(newKey);
            HasKeys = true;
            UpdateFileFilters();
            StatusMessage = $"Added new key '{newKey.Key}' to {newKey.Source.Name}.";
            LanguagesChanged?.Invoke(this, EventArgs.Empty);

            // Auto-save after adding key
            SaveProgress();
        }
    }

    private void UpdateFileFilters()
    {
        // Keep the selected file if it still exists
        var currentSelection = SelectedSourceFile;

        AvailableSourceFiles.Clear();

        // Add "All Files" option (null)
        AvailableSourceFiles.Add(null);

        // Add all source files
        foreach (var sourceFile in _translationStore.SourceFiles.OrderBy(f => f.Name).ThenBy(f => f.Type))
        {
            AvailableSourceFiles.Add(sourceFile);
        }

        // Restore selection if it still exists, otherwise select "All Files"
        if (currentSelection != null && _translationStore.SourceFiles.Contains(currentSelection))
        {
            SelectedSourceFile = currentSelection;
        }
        else
        {
            SelectedSourceFile = null;
        }
    }

    partial void OnSelectedSourceFileChanged(SourceFile? value)
    {
        if (value == null)
        {
            // Show all files
            _translationStore.FilterBySourceFiles(null!);
        }
        else
        {
            // Show only selected file
            _translationStore.FilterBySourceFiles(new List<SourceFile> { value });
        }

        UpdateStatusMessage();
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SelectedSourceFile = null;
        SearchText = string.Empty;
        _translationStore.FilterBySourceFiles(null!);
        _translationStore.FilterBySearchTerm(string.Empty);
        UpdateStatusMessage();
    }

    partial void OnSearchTextChanged(string value)
    {
        _translationStore.FilterBySearchTerm(value);
        UpdateStatusMessage();
    }

    private void UpdateStatusMessage()
    {
        var filteredCount = _translationStore.FilteredKeys.Count;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = $"Found {filteredCount} translation key(s) matching '{SearchText}'.";
        }
        else if (SelectedSourceFile != null)
        {
            StatusMessage = $"Showing {filteredCount} translation keys from {SelectedSourceFile.Name} ({SelectedSourceFile.Type}).";
        }
        else
        {
            StatusMessage = $"Showing {filteredCount} translation keys.";
        }
    }

    [RelayCommand]
    private async Task EditTranslation(TranslationKey translationKey)
    {
        // Window will be accessed through the view
        OnEditTranslationRequested?.Invoke(this, translationKey);
    }

    [RelayCommand]
    private async Task ExportModifiedFiles(Window window)
    {
        var allKeys = _translationStore.GetAllKeys();

        if (allKeys.Count == 0)
        {
            StatusMessage = "No translations to export. Import files first.";
            return;
        }

        // Default to output directory in current working directory
        var defaultOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "output");

        // Ensure the default directory exists for the folder picker
        if (!Directory.Exists(defaultOutputPath))
        {
            Directory.CreateDirectory(defaultOutputPath);
        }

        var folder = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Export Destination",
            AllowMultiple = false,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(defaultOutputPath)
        });

        if (folder.Count == 0)
        {
            return;
        }

        var outputPath = folder[0].Path.LocalPath;

        // Group all keys by file type and export using appropriate writer
        var jsonKeys = allKeys.Where(k => k.Source.Type == FileType.Json).ToList();
        var resxKeys = allKeys.Where(k => k.Source.Type == FileType.Resx).ToList();

        if (jsonKeys.Count > 0)
        {
            // Provide template provider function to preserve original JSON structure
            _jsonWriter.WriteFiles(jsonKeys, outputPath, sourceFileName => _translationStore.GetJsonTemplate(sourceFileName));
        }

        if (resxKeys.Count > 0)
        {
            // Provide template provider function to preserve original RESX structure
            _resxWriter.WriteFiles(resxKeys, outputPath, sourceFileName => _translationStore.GetResxTemplate(sourceFileName));
        }

        StatusMessage = $"Exported {allKeys.Count} translation key(s) to {outputPath}.";

        // Prompt user for next action after export
        await PromptAfterExport(window);
    }

    private async Task PromptAfterExport(Window window)
    {
        var dialog = new Window
        {
            Title = "Export Complete",
            Width = 450,
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
            Text = "Files exported successfully! What would you like to do next?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var continueButton = new Button { Content = "Continue Working", Width = 150 };
        var startOverButton = new Button { Content = "Start Over", Width = 150 };

        bool startOver = false;

        continueButton.Click += (s, args) => { startOver = false; dialog.Close(); };
        startOverButton.Click += (s, args) => { startOver = true; dialog.Close(); };

        buttonPanel.Children.Add(continueButton);
        buttonPanel.Children.Add(startOverButton);

        panel.Children.Add(new TextBlock
        {
            Text = "Continue Working: Keep current translations to make more changes\nStart Over: Clear everything and import new files",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 11,
            Foreground = Avalonia.Media.Brushes.Gray
        });

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(window);

        if (startOver)
        {
            await StartOver();
        }
    }

    private string ExtractBaseFileName(string filePath, FileType fileType)
    {
        return fileType == FileType.Json
            ? _jsonReader.ExtractBaseFileName(filePath)
            : _resxReader.ExtractBaseFileName(filePath);
    }

    [RelayCommand]
    private void SaveProgress()
    {
        var sessionState = new SessionState
        {
            TranslationKeys = _translationStore.GetAllKeys(),
            ImportedFileNames = ImportedFileNames.ToList(),
            ResxTemplates = _translationStore.GetAllResxTemplates(),
            JsonTemplates = _translationStore.GetAllJsonTemplates(),
            CurrentStep = CurrentStep,
            ImportStepStatus = ImportStepStatus,
            EditStepStatus = EditStepStatus,
            ExportStepStatus = ExportStepStatus
        };
        
        _progressService.SaveProgress(sessionState);
        _translationStore.MarkAllChangesSaved();
        StatusMessage = "Progress saved successfully.";
    }

    [RelayCommand]
    private async Task StartOver()
    {
        var window = App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (window == null) return;

        // Show confirmation dialog
        var dialog = new Window
        {
            Title = "Confirm Start Over",
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 20
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Are you sure you want to start over? This will clear all translations and delete saved progress.",
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10
        };

        var confirmButton = new Button { Content = "Yes, Start Over", Width = 130 };
        var cancelButton = new Button { Content = "Cancel", Width = 100 };

        bool confirmed = false;

        confirmButton.Click += (s, args) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, args) => { confirmed = false; dialog.Close(); };

        buttonPanel.Children.Add(confirmButton);
        buttonPanel.Children.Add(cancelButton);

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(window);

        if (!confirmed) return;

        // Proceed with start over
        _translationStore.Clear();
        _progressService.ClearProgress();
        ImportedFileNames.Clear();
        HasKeys = false;
        HasUnsavedChanges = false;
        
        // Reset workflow state
        CurrentStep = WorkflowStep.Import;
        ImportStepStatus = StepStatus.InProgress;
        EditStepStatus = StepStatus.NotStarted;
        ExportStepStatus = StepStatus.NotStarted;
        
        UpdateFileFilters();
        StatusMessage = "Ready. Click Import to load translation files.";
    }

    [RelayCommand]
    private void ConfirmImport()
    {
        if (!HasKeys)
        {
            StatusMessage = "Please import at least one file before continuing.";
            return;
        }

        ImportStepStatus = StepStatus.Completed;
        EditStepStatus = StepStatus.InProgress;
        CurrentStep = WorkflowStep.Edit;
        StatusMessage = "Import complete. You can now edit translations or proceed to export.";
        
        // Auto-save progress
        SaveProgress();
    }

    [RelayCommand]
    private void CompleteEdit()
    {
        EditStepStatus = StepStatus.Completed;
        ExportStepStatus = StepStatus.InProgress;
        CurrentStep = WorkflowStep.Export;
        StatusMessage = "Ready to export translations.";
        
        // Auto-save progress
        SaveProgress();
    }

    [RelayCommand]
    private void GoBackToEdit()
    {
        ExportStepStatus = StepStatus.NotStarted;
        EditStepStatus = StepStatus.InProgress;
        CurrentStep = WorkflowStep.Edit;
        StatusMessage = "Returned to editing. Make your changes and proceed to export when ready.";
        
        // Auto-save progress
        SaveProgress();
    }

    [RelayCommand]
    private async Task ExportToZip()
    {
        var allKeys = _translationStore.GetAllKeys();

        if (allKeys.Count == 0)
        {
            StatusMessage = "No translations to export.";
            return;
        }

        // Create output directory if it doesn't exist
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output");
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // Create timestamped folder name
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var tempFolderName = $"exported_translations_{timestamp}";
        var tempFolderPath = Path.Combine(Path.GetTempPath(), tempFolderName);
        var zipFilePath = Path.Combine(outputPath, $"{tempFolderName}.zip");

        try
        {
            // Create temporary folder for exported files
            Directory.CreateDirectory(tempFolderPath);

            // Group all keys by file type and export to temp folder
            var jsonKeys = allKeys.Where(k => k.Source.Type == FileType.Json).ToList();
            var resxKeys = allKeys.Where(k => k.Source.Type == FileType.Resx).ToList();

            if (jsonKeys.Count > 0)
            {
                _jsonWriter.WriteFiles(jsonKeys, tempFolderPath, sourceFileName => _translationStore.GetJsonTemplate(sourceFileName));
            }

            if (resxKeys.Count > 0)
            {
                _resxWriter.WriteFiles(resxKeys, tempFolderPath, sourceFileName => _translationStore.GetResxTemplate(sourceFileName));
            }

            // Create ZIP file
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }
            ZipFile.CreateFromDirectory(tempFolderPath, zipFilePath);

            // Clean up temp folder
            Directory.Delete(tempFolderPath, true);

            ExportStepStatus = StepStatus.Completed;
            StatusMessage = $"Exported {allKeys.Count} translation key(s) to {zipFilePath}";

            // Store output folder and filename for dialog
            _lastExportFolder = outputPath;
            _lastExportFileName = Path.GetFileName(zipFilePath);

            // Prompt to start over
            await PromptToStartOverAfterExport();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private async Task PromptToStartOverAfterExport()
    {
        var window = App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (window == null) return;

        var dialog = new Window
        {
            Title = "Export Complete",
            Width = 450,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 15
        };

        // Success message
        var successPanel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E8F5E9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#4CAF50")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 5)
        };

        var successStack = new StackPanel { Spacing = 8 };
        successStack.Children.Add(new TextBlock
        {
            Text = "✓ Export Successful!",
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#2E7D32")),
            FontSize = 14
        });

        successStack.Children.Add(new TextBlock
        {
            Text = $"File: {_lastExportFileName}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#424242")),
            FontWeight = FontWeight.Medium
        });

        successStack.Children.Add(new TextBlock
        {
            Text = $"Location: output/",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#757575"))
        });

        successPanel.Child = successStack;
        panel.Children.Add(successPanel);

        // View in Finder/Explorer button
        var viewFolderButton = new Button
        {
            Content = OperatingSystem.IsMacOS() ? "📁 View in Finder" : "📁 View in Explorer",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(15, 8),
            Margin = new Thickness(0, 0, 0, 10)
        };

        viewFolderButton.Click += (s, args) =>
        {
            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = _lastExportFolder,
                        UseShellExecute = false
                    });
                }
                else if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _lastExportFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = _lastExportFolder,
                        UseShellExecute = false
                    });
                }
            }
            catch
            {
                // Silently ignore if opening fails
            }
        };

        panel.Children.Add(viewFolderButton);

        panel.Children.Add(new TextBlock
        {
            Text = "Would you like to start a new session?",
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Starting over will clear all current translations and reset the workflow.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10
        };

        var startOverButton = new Button { Content = "Start New Session", Width = 150 };
        var continueButton = new Button { Content = "Stay Here", Width = 120 };

        bool shouldStartOver = false;

        startOverButton.Click += (s, args) => { shouldStartOver = true; dialog.Close(); };
        continueButton.Click += (s, args) => { shouldStartOver = false; dialog.Close(); };

        buttonPanel.Children.Add(continueButton);
        buttonPanel.Children.Add(startOverButton);

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(window);

        if (shouldStartOver)
        {
            await StartOver();
            // Reset workflow
            CurrentStep = WorkflowStep.Import;
            ImportStepStatus = StepStatus.InProgress;
            EditStepStatus = StepStatus.NotStarted;
            ExportStepStatus = StepStatus.NotStarted;
        }
    }

    private void LoadProgress()
    {
        var sessionState = _progressService.LoadProgress();
        if (sessionState != null && sessionState.TranslationKeys.Count > 0)
        {
            // Restore translation keys
            _translationStore.AddTranslations(sessionState.TranslationKeys);
            
            // Restore templates
            _translationStore.RestoreResxTemplates(sessionState.ResxTemplates);
            _translationStore.RestoreJsonTemplates(sessionState.JsonTemplates);
            
            // Restore imported file names
            ImportedFileNames.Clear();
            foreach (var fileName in sessionState.ImportedFileNames)
            {
                ImportedFileNames.Add(fileName);
            }
            
            // Restore workflow state
            CurrentStep = sessionState.CurrentStep;
            ImportStepStatus = sessionState.ImportStepStatus;
            EditStepStatus = sessionState.EditStepStatus;
            ExportStepStatus = sessionState.ExportStepStatus;
            
            UpdateFileFilters();
            HasKeys = true;
            HasUnsavedChanges = false;
            
            StatusMessage = $"Loaded {sessionState.TranslationKeys.Count} translation keys from saved progress.";
            LanguagesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool PromptToSaveChanges()
    {
        // Return true if it's safe to close (no unsaved changes or user chose to discard)
        // Return false if user wants to cancel closing
        return !HasUnsavedChanges;
    }

    public event EventHandler<TranslationKey>? OnEditTranslationRequested;

    private void LoadUserSettings()
    {
        var settings = _settingsService.Load();
        if (settings != null && !string.IsNullOrWhiteSpace(settings.Username))
        {
            Username = settings.Username;
        }
    }

    [RelayCommand]
    private async Task ShowProfile(Window window)
    {
        // Create profile dialog
        var dialog = new Window
        {
            Title = "User Profile",
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var mainPanel = new DockPanel
        {
            Margin = new Thickness(20),
            LastChildFill = true
        };

        // Username section
        var contentPanel = new StackPanel
        {
            Spacing = 10
        };

        contentPanel.Children.Add(new TextBlock
        {
            Text = "Username",
            FontWeight = FontWeight.SemiBold
        });

        var usernameBox = new TextBox
        {
            Text = Username
        };
        contentPanel.Children.Add(usernameBox);

        DockPanel.SetDock(contentPanel, Dock.Top);
        mainPanel.Children.Add(contentPanel);

        // Buttons at bottom right
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 15, 0, 0)
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        var saveButton = new Button
        {
            Content = "Save",
            Width = 80,
            IsEnabled = false,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        // Enable save button only when username changes
        usernameBox.TextChanged += (s, e) =>
        {
            var newText = usernameBox.Text?.Trim() ?? "";
            saveButton.IsEnabled = !string.IsNullOrWhiteSpace(newText) && newText != Username;
        };

        saveButton.Click += (s, args) =>
        {
            var newUsername = usernameBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(newUsername) && newUsername != Username)
            {
                Username = newUsername;
                var settings = new UserSettings { Username = newUsername };
                _settingsService.Save(settings);
                StatusMessage = $"Username updated to '{newUsername}'.";
            }
            dialog.Close();
        };

        cancelButton.Click += (s, args) => { dialog.Close(); };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(saveButton);

        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(new Border()); // Filler

        dialog.Content = mainPanel;
        await dialog.ShowDialog(window);
    }
}

