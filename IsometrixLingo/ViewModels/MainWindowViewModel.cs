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
using IsometrixLingo.Helpers;
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

    [ObservableProperty]
    private ObservableCollection<string> _ignoredFileNames = new();

    // Workflow state properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowImportStep), nameof(ShowFileMappingStep), nameof(ShowModeSelectionStep), nameof(ShowEditStep), nameof(ShowExportStep),
                               nameof(Step1Background), nameof(Step2Background), nameof(Step3Background), nameof(Step4Background), nameof(Step5Background),
                               nameof(Step1Status), nameof(Step2Status), nameof(Step3Status), nameof(Step4Status), nameof(Step5Status))]
    private WorkflowStep _currentStep = WorkflowStep.Import;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step1Background), nameof(Step1Foreground), nameof(Step1Status))]
    private StepStatus _importStepStatus = StepStatus.InProgress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step2Background), nameof(Step2Foreground), nameof(Step2Status))]
    private StepStatus _fileMappingStepStatus = StepStatus.NotStarted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step3Background), nameof(Step3Foreground), nameof(Step3Status))]
    private StepStatus _modeSelectionStepStatus = StepStatus.NotStarted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step4Background), nameof(Step4Foreground), nameof(Step4Status))]
    private StepStatus _editStepStatus = StepStatus.NotStarted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step5Background), nameof(Step5Foreground), nameof(Step5Status), nameof(StartOverButtonText))]
    private StepStatus _exportStepStatus = StepStatus.NotStarted;

    [ObservableProperty]
    private bool _showOriginalValues;

    [ObservableProperty]
    private bool _showOnlyMissingTranslations;

    [ObservableProperty]
    private bool _showFilters = true;

    [ObservableProperty]
    private ObservableCollection<FilePair> _filePairs = new();

    [ObservableProperty]
    private EditMode _currentMode = EditMode.Edit;

    public bool ShowImportStep => CurrentStep == WorkflowStep.Import;
    public bool ShowFileMappingStep => CurrentStep == WorkflowStep.FileMapping;
    public bool ShowModeSelectionStep => CurrentStep == WorkflowStep.ModeSelection;
    public bool ShowEditStep => CurrentStep == WorkflowStep.Edit;
    public bool ShowExportStep => CurrentStep == WorkflowStep.Export;

    public string StartOverButtonText => ExportStepStatus == StepStatus.Completed ? "Start New Session" : "Start Over";

    public SolidColorBrush Step1Background => ImportStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step2Background => FileMappingStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step3Background => ModeSelectionStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step4Background => EditStepStatus switch
    {
        StepStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
        StepStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Medium gray
    };

    public SolidColorBrush Step5Background => ExportStepStatus switch
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

    public SolidColorBrush Step2Foreground => FileMappingStepStatus switch
    {
        StepStatus.NotStarted => new SolidColorBrush(Colors.White),
        _ => new SolidColorBrush(Colors.White)
    };

    public SolidColorBrush Step3Foreground => ModeSelectionStepStatus switch
    {
        StepStatus.NotStarted => new SolidColorBrush(Colors.White),
        _ => new SolidColorBrush(Colors.White)
    };

    public SolidColorBrush Step4Foreground => EditStepStatus switch
    {
        StepStatus.NotStarted => new SolidColorBrush(Colors.White),
        _ => new SolidColorBrush(Colors.White)
    };

    public SolidColorBrush Step5Foreground => ExportStepStatus switch
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

    public string Step2Status => FileMappingStepStatus switch
    {
        StepStatus.Completed => "✓ Complete",
        StepStatus.InProgress => "In Progress",
        _ => "Not Started"
    };

    public string Step3Status => ModeSelectionStepStatus switch
    {
        StepStatus.Completed => "✓ Complete",
        StepStatus.InProgress => "In Progress",
        _ => "Not Started"
    };

    public string Step4Status => EditStepStatus switch
    {
        StepStatus.Completed => "✓ Complete",
        StepStatus.InProgress => "In Progress",
        _ => "Not Started"
    };

    public string Step5Status => ExportStepStatus switch
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
        try
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
            // Don't clear existing files - we want to accumulate multiple imports
            // ImportedFileNames.Clear(); // REMOVED to allow multiple imports

            foreach (var file in files)
            {
                try
                {
                    var filePath = file.Path.LocalPath;
                    var fileName = Path.GetFileName(filePath);
                    var extension = Path.GetExtension(filePath).ToLower();

                    // Check for duplicate files (case-insensitive)
                    if (ImportedFileNames.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)) ||
                        IgnoredFileNames.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"File '{fileName}' has already been imported. Skipping duplicate.";
                        continue;
                    }

                    // Check if language is supported (en or es only)
                    var fileType = extension == ".json" ? FileType.Json : FileType.Resx;
                    var language = ExtractLanguage(fileName, fileType);
                    
                    if (string.IsNullOrEmpty(language) || (language != "en" && language != "es"))
                    {
                        IgnoredFileNames.Add(fileName);
                        StatusMessage = $"File '{fileName}' ignored - unsupported language. Only English (en) and Spanish (es) are supported.";
                        continue;
                    }

                    TranslationFile translationFile = extension switch
                    {
                        ".json" => _jsonReader.ReadFile(filePath),
                        ".resx" => _resxReader.ReadFile(filePath),
                        _ => throw new NotSupportedException($"Unsupported file type: {extension}")
                    };

                    translationFiles.Add(translationFile);
                    ImportedFileNames.Add(fileName);
                }
                catch (Exception ex)
                {
                    // Log individual file errors but continue processing
                    StatusMessage = $"Warning: Could not import file - {ex.Message}";
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
            StatusMessage = $"Imported {translationFiles.Count} file(s). Review the imported files below and click 'Confirm & Continue' to proceed.";
            HasKeys = _translationStore.GetAllKeys().Count > 0;
            ImportStepStatus = StepStatus.InProgress;
            LanguagesChanged?.Invoke(this, EventArgs.Empty);

            // Auto-save imported progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing files: {ex.Message}";
            // Don't re-throw - keep the app running
        }
    }

    public async Task ImportDroppedFiles(IEnumerable<IStorageItem> storageItems)
    {
        try
        {
            var files = storageItems.ToList();

            if (files.Count == 0)
            {
                return;
            }

            var translationFiles = new List<TranslationFile>();
            // Don't clear existing files - we want to accumulate multiple drops
            // ImportedFileNames.Clear(); // REMOVED to allow multiple drops

            foreach (var item in files)
            {
                if (item is not IStorageFile file)
                    continue;

                try
                {
                    var filePath = file.Path.LocalPath;
                    var fileName = Path.GetFileName(filePath);
                    var extension = Path.GetExtension(filePath).ToLower();

                    // Only process JSON and RESX files
                    if (extension != ".json" && extension != ".resx")
                        continue;

                    // Check for duplicate files (case-insensitive)
                    if (ImportedFileNames.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)) ||
                        IgnoredFileNames.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"File '{fileName}' has already been imported. Skipping duplicate.";
                        continue;
                    }

                    // Check if language is supported (en or es only)
                    var fileType = extension == ".json" ? FileType.Json : FileType.Resx;
                    var language = ExtractLanguage(fileName, fileType);
                    
                    if (string.IsNullOrEmpty(language) || (language != "en" && language != "es"))
                    {
                        IgnoredFileNames.Add(fileName);
                        StatusMessage = $"File '{fileName}' ignored - unsupported language. Only English (en) and Spanish (es) are supported.";
                        continue;
                    }

                    TranslationFile translationFile = extension switch
                    {
                        ".json" => _jsonReader.ReadFile(filePath),
                        ".resx" => _resxReader.ReadFile(filePath),
                        _ => throw new NotSupportedException($"Unsupported file type: {extension}")
                    };

                    translationFiles.Add(translationFile);
                    ImportedFileNames.Add(fileName);
                }
                catch (Exception ex)
                {
                    // Log individual file errors but continue processing
                    StatusMessage = $"Warning: Could not import file - {ex.Message}";
                }
            }

            if (translationFiles.Count == 0)
            {
                StatusMessage = "No valid translation files found. Please drop JSON or RESX files.";
                return;
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
            StatusMessage = $"Imported {translationFiles.Count} file(s). Review the imported files below and click 'Confirm & Continue' to proceed.";
            HasKeys = _translationStore.GetAllKeys().Count > 0;
            ImportStepStatus = StepStatus.InProgress;
            LanguagesChanged?.Invoke(this, EventArgs.Empty);

            // Auto-save imported progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing files: {ex.Message}";
            // Don't re-throw - keep the app running
        }
    }

    [RelayCommand]
    private async Task AddKey(Window window)
    {
        var addKeyViewModel = new AddKeyViewModel(
            _translationStore.SourceFiles,
            _translationStore.Languages,
            CurrentMode,
            Username,
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
            
            var modeText = CurrentMode == EditMode.Edit ? "values" : "suggestions";
            StatusMessage = $"Added new key '{newKey.Key}' with {modeText} to {newKey.Source.Name}.";
            LanguagesChanged?.Invoke(this, EventArgs.Empty);
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
        ShowOriginalValues = false;
        ShowOnlyMissingTranslations = false;
        _translationStore.FilterBySourceFiles(null!);
        _translationStore.FilterBySearchTerm(string.Empty);
        UpdateStatusMessage();
    }

    [RelayCommand]
    private void ToggleFilters()
    {
        ShowFilters = !ShowFilters;
    }

    partial void OnSearchTextChanged(string value)
    {
        _translationStore.FilterBySearchTerm(value);
        UpdateStatusMessage();
    }

    partial void OnShowOnlyMissingTranslationsChanged(bool value)
    {
        _translationStore.FilterByMissingTranslations(value);
        UpdateStatusMessage();
    }

    partial void OnShowOriginalValuesChanged(bool value)
    {
        // Set all per-row toggles to match the global toggle
        foreach (var key in _translationStore.GetAllKeys())
        {
            key.ShowOriginalForThisRow = value;
        }
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

        // Detect new files that will be created
        var newFiles = DetectNewFilesForExport(allKeys);
        
        // If there are new files, show confirmation
        if (newFiles.Count > 0)
        {
            var confirmed = await ShowNewFilesConfirmation(window, newFiles);
            if (!confirmed)
            {
                StatusMessage = "Export cancelled.";
                return;
            }
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

    private List<string> DetectNewFilesForExport(List<TranslationKey> allKeys)
    {
        var newFiles = new List<string>();
        var importedFileNamesLower = new HashSet<string>(
            ImportedFileNames.Select(f => f.ToLower()), 
            StringComparer.OrdinalIgnoreCase
        );

        // Group keys by source file
        var groupedByFile = allKeys.GroupBy(k => k.Source.Name);

        foreach (var fileGroup in groupedByFile)
        {
            var sourceFile = fileGroup.Key;
            var fileKeys = fileGroup.ToList();
            var fileType = fileKeys.First().Source.Type;

            // Get all languages for this file
            var languages = fileKeys
                .SelectMany(k => k.LanguageValues.Keys)
                .Distinct()
                .ToList();

            // Check each language file
            foreach (var language in languages)
            {
                string fileName;
                if (fileType == FileType.Json)
                {
                    fileName = $"{sourceFile}.{language}.json";
                }
                else // RESX
                {
                    fileName = language == "en" 
                        ? $"{sourceFile}.resx" 
                        : $"{sourceFile}_{language}.resx";
                }

                if (!importedFileNamesLower.Contains(fileName.ToLower()))
                {
                    newFiles.Add(fileName);
                }
            }
        }

        return newFiles;
    }

    private async Task<bool> ShowNewFilesConfirmation(Window window, List<string> newFiles)
    {
        var dialog = new Window
        {
            Title = "Confirm New Files",
            Width = 500,
            Height = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var mainPanel = new DockPanel
        {
            Margin = new Thickness(20),
            LastChildFill = true
        };

        // Header
        var headerPanel = new StackPanel { Spacing = 10 };
        DockPanel.SetDock(headerPanel, Dock.Top);

        headerPanel.Children.Add(new TextBlock
        {
            Text = "⚠ New Files Will Be Created",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)) // Orange
        });

        headerPanel.Children.Add(new TextBlock
        {
            Text = "The following files did not exist in your original import but will be created during export:",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        mainPanel.Children.Add(headerPanel);

        // Files list in scrollable area
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 10, 0, 15)
        };
        DockPanel.SetDock(scrollViewer, Dock.Top);

        var filesList = new StackPanel { Spacing = 5 };
        foreach (var file in newFiles)
        {
            filesList.Children.Add(new TextBlock
            {
                Text = $"• {file}",
                FontFamily = new FontFamily("Courier New, monospace"),
                Margin = new Thickness(10, 0, 0, 0)
            });
        }

        scrollViewer.Content = filesList;
        mainPanel.Children.Add(scrollViewer);

        // Confirmation message
        var confirmPanel = new StackPanel { Spacing = 10 };
        DockPanel.SetDock(confirmPanel, Dock.Top);

        confirmPanel.Children.Add(new TextBlock
        {
            Text = "Do you want to proceed with creating these files?",
            FontWeight = FontWeight.SemiBold
        });

        mainPanel.Children.Add(confirmPanel);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 15, 0, 0)
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        var proceedButton = new Button
        {
            Content = "Yes, Create Files",
            Width = 130,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        bool confirmed = false;

        proceedButton.Click += (s, args) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, args) => { confirmed = false; dialog.Close(); };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(proceedButton);

        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(new Border()); // Filler

        dialog.Content = mainPanel;
        await dialog.ShowDialog(window);

        return confirmed;
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
        try
        {
            var sessionState = new SessionState
            {
                TranslationKeys = _translationStore.GetAllKeys(),
                ImportedFileNames = ImportedFileNames.ToList(),
                ResxTemplates = _translationStore.GetAllResxTemplates(),
                JsonTemplates = _translationStore.GetAllJsonTemplates(),
                CurrentStep = CurrentStep,
                ImportStepStatus = ImportStepStatus,
                FileMappingStepStatus = FileMappingStepStatus,
                ModeSelectionStepStatus = ModeSelectionStepStatus,
                EditStepStatus = EditStepStatus,
                ExportStepStatus = ExportStepStatus,
                CurrentMode = CurrentMode
            };

            _progressService.SaveProgress(sessionState);
            _translationStore.MarkAllChangesSaved();
            StatusMessage = "Progress saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Warning: Could not save progress - {ex.Message}";
            // Don't re-throw - saving progress is not critical enough to crash the app
        }
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

        var confirmButton = new Button
        {
            Content = "Yes, Start Over",
            Width = 130,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

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
        IgnoredFileNames.Clear();
        FilePairs.Clear();
        HasKeys = false;
        HasUnsavedChanges = false;

        // Reset workflow state
        CurrentStep = WorkflowStep.Import;
        ImportStepStatus = StepStatus.InProgress;
        FileMappingStepStatus = StepStatus.NotStarted;
        EditStepStatus = StepStatus.NotStarted;
        ExportStepStatus = StepStatus.NotStarted;

        UpdateFileFilters();
        StatusMessage = "Ready. Click Import to load translation files.";
    }

    [RelayCommand]
    private void ConfirmImport()
    {
        try
        {
            if (!HasKeys)
            {
                StatusMessage = "Please import at least one file before continuing.";
                return;
            }

            ImportStepStatus = StepStatus.Completed;
            FileMappingStepStatus = StepStatus.InProgress;
            CurrentStep = WorkflowStep.FileMapping;

            // Generate file pairs
            DetectFilePairs();

            StatusMessage = "Import complete. Review file mappings and confirm to continue.";

            // Auto-save progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error confirming import: {ex.Message}";
            // Don't re-throw - keep the app running
        }
    }

    [RelayCommand]
    private async Task CompleteEdit(Window? window)
    {
        try
        {
            // Get the window if not provided
            if (window == null)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    window = desktop.MainWindow;
                }
            }

            if (window == null)
            {
                StatusMessage = "Cannot show dialog - no window available.";
                return;
            }

            // Check for unresolved suggestions
            var keysWithSuggestions = _translationStore.GetAllKeys()
                .Where(k => k.HasAnySuggestions)
                .ToList();

            if (keysWithSuggestions.Count > 0)
            {
                var totalSuggestions = keysWithSuggestions.Sum(k => k.SuggestedValues.Count);
                var message = $"There are {totalSuggestions} unresolved suggestion(s) across {keysWithSuggestions.Count} key(s).\n\n" +
                              "These suggestions have not been accepted or rejected.\n\n" +
                              "Do you want to stay and review the suggestions, or continue to export anyway?";

                var dialog = new ConfirmationDialog(message);
                var result = await dialog.ShowDialog<bool?>(window);

                if (result != true)
                {
                    StatusMessage = "Review suggestions before exporting.";
                    return;
                }
            }

            // Check for missing translations
            var keysWithMissingTranslations = _translationStore.GetAllKeys()
                .Where(k => k.HasMissingTranslations)
                .ToList();

            if (keysWithMissingTranslations.Count > 0)
            {
                var message = $"There are {keysWithMissingTranslations.Count} translation key(s) with missing terms.\n\n" +
                              "Do you want to stay and add the missing translations, or continue to export anyway?";

                var dialog = new ConfirmationDialog(message);
                var result = await dialog.ShowDialog<bool?>(window);

                // If result is null (dialog closed) or false (stay clicked), don't proceed
                if (result != true)
                {
                    // User chose to stay and add missing terms
                    StatusMessage = "Add missing translations before exporting.";
                    return;
                }
            }

            EditStepStatus = StepStatus.Completed;
            ExportStepStatus = StepStatus.InProgress;
            CurrentStep = WorkflowStep.Export;
            StatusMessage = "Ready to export translations.";

            // Auto-save progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error completing edit step: {ex.Message}";
            // Don't re-throw - keep the app running
        }
    }

    [RelayCommand]
    private void GoBackToEdit()
    {
        try
        {
            ExportStepStatus = StepStatus.NotStarted;
            EditStepStatus = StepStatus.InProgress;
            CurrentStep = WorkflowStep.Edit;
            StatusMessage = "Returned to editing. Make your changes and proceed to export when ready.";

            // Auto-save progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error returning to edit step: {ex.Message}";
            // Don't re-throw - keep the app running
        }
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

        // Create output directory in user's Documents folder
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var outputPath = Path.Combine(documentsPath, "IsometrixLingo", "output");
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

            // Auto-save completion of all steps
            SaveProgress();

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
            HorizontalContentAlignment = HorizontalAlignment.Center,
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

        var startOverButton = new Button
        {
            Content = "Start New Session",
            Width = 150,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        var continueButton = new Button
        {
            Content = "Stay Here",
            Width = 120,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

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
            FileMappingStepStatus = sessionState.FileMappingStepStatus;
            ModeSelectionStepStatus = sessionState.ModeSelectionStepStatus;
            EditStepStatus = sessionState.EditStepStatus;
            ExportStepStatus = sessionState.ExportStepStatus;
            CurrentMode = sessionState.CurrentMode;

            // Regenerate file pairs if we're on the FileMapping step
            if (CurrentStep == WorkflowStep.FileMapping || FileMappingStepStatus != StepStatus.NotStarted)
            {
                DetectFilePairs();
            }

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

    private void DetectFilePairs()
    {
        FilePairs.Clear();

        // Group imported files by base name and file type
        var fileGroups = ImportedFileNames
            .GroupBy(fileName =>
            {
                var extension = Path.GetExtension(fileName).ToLower();
                var fileType = extension == ".json" ? FileType.Json : FileType.Resx;
                var baseName = ExtractBaseName(fileName, fileType);
                return new { BaseName = baseName, FileType = fileType };
            })
            .ToList();

        foreach (var group in fileGroups)
        {
            var pair = new FilePair
            {
                BaseName = group.Key.BaseName,
                FileType = group.Key.FileType
            };

            // Find English and Spanish files in the group
            foreach (var fileName in group)
            {
                var language = ExtractLanguage(fileName, group.Key.FileType);
                if (language == "en")
                {
                    pair.EnglishFileName = fileName;
                    pair.HasEnglishFile = true;
                }
                else if (language == "es")
                {
                    pair.SpanishFileName = fileName;
                    pair.HasSpanishFile = true;
                }
            }

            FilePairs.Add(pair);
        }
    }

    private string ExtractBaseName(string fileName, FileType fileType)
    {
        if (fileType == FileType.Json)
        {
            // Remove .en.json or .es.json
            return fileName.Replace(".en.json", "").Replace(".es.json", "");
        }
        else // RESX
        {
            // Remove _es.resx or .resx
            return fileName.Replace("_es.resx", "").Replace(".resx", "");
        }
    }

    private string ExtractLanguage(string fileName, FileType fileType)
    {
        if (fileType == FileType.Json)
        {
            if (fileName.Contains(".en.json", StringComparison.OrdinalIgnoreCase))
                return "en";
            if (fileName.Contains(".es.json", StringComparison.OrdinalIgnoreCase))
                return "es";
        }
        else // RESX
        {
            if (fileName.Contains("_es.resx", StringComparison.OrdinalIgnoreCase))
                return "es";
            if (fileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase) &&
                !fileName.Contains("_es.resx", StringComparison.OrdinalIgnoreCase))
                return "en";
        }

        return string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmFileMapping(Window? window)
    {
        try
        {
            // Create missing files if requested
            foreach (var pair in FilePairs)
            {
                if (pair.CreateMissingEnglish && !pair.HasEnglishFile)
                {
                    await CreateMissingFile(pair, "en");
                }

                if (pair.CreateMissingSpanish && !pair.HasSpanishFile)
                {
                    await CreateMissingFile(pair, "es");
                }
            }

            FileMappingStepStatus = StepStatus.Completed;
            ModeSelectionStepStatus = StepStatus.InProgress;
            CurrentStep = WorkflowStep.ModeSelection;
            StatusMessage = "File mapping confirmed. Please select your editing mode.";

            // Auto-save progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error confirming file mapping: {ex.Message}";
            // Don't re-throw - keep the app running
        }
    }

    [RelayCommand]
    private void SelectMode(EditMode mode)
    {
        CurrentMode = mode;
        ModeSelectionStepStatus = StepStatus.Completed;
        EditStepStatus = StepStatus.InProgress;
        CurrentStep = WorkflowStep.Edit;
        
        var modeText = mode == EditMode.Edit ? "Edit" : "Suggest";
        StatusMessage = $"{modeText} mode selected. You can now {modeText.ToLower()} translations.";

        // Auto-save progress
        SaveProgress();
    }

    private async Task CreateMissingFile(FilePair pair, string language)
    {
        // Create a new translation file with empty values for all keys from the existing pair file
        var existingFileName = language == "en" ? pair.SpanishFileName : pair.EnglishFileName;
        if (string.IsNullOrEmpty(existingFileName))
            return;

        // Generate the missing file name
        string newFileName;
        if (pair.FileType == FileType.Json)
        {
            newFileName = $"{pair.BaseName}.{language}.json";
        }
        else // RESX
        {
            newFileName = language == "es" ? $"{pair.BaseName}_es.resx" : $"{pair.BaseName}.resx";
        }

        // Get all keys from the existing file
        var existingKeys = _translationStore.GetAllKeys()
            .Where(k => k.Source.Name == pair.BaseName && k.Source.Type == pair.FileType)
            .ToList();

        // Create new translation file entries with empty values
        foreach (var key in existingKeys)
        {
            // Add empty value for the new language
            if (!key.LanguageValues.ContainsKey(language))
            {
                var newValues = new Dictionary<string, string>(key.LanguageValues)
                {
                    [language] = string.Empty
                };
                key.LanguageValues = newValues;
                key.UpdateMissingTranslationsStatus();
            }
        }

        // Add to imported file names
        if (!ImportedFileNames.Any(f => string.Equals(f, newFileName, StringComparison.OrdinalIgnoreCase)))
        {
            ImportedFileNames.Add(newFileName);
        }

        // Update the pair
        if (language == "en")
        {
            pair.EnglishFileName = newFileName;
            pair.HasEnglishFile = true;
        }
        else
        {
            pair.SpanishFileName = newFileName;
            pair.HasSpanishFile = true;
        }

        StatusMessage = $"Created empty file: {newFileName}";
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

    [RelayCommand]
    private void AcceptSuggestion((TranslationKey key, string language) parameters)
    {
        var (key, language) = parameters;
        
        if (!key.SuggestedValues.TryGetValue(language, out var suggestion))
            return;

        // Apply the suggestion to the actual value
        key.LanguageValues[language] = suggestion.Value;
        
        // Remove the suggestion
        key.SuggestedValues.Remove(language);
        
        // Mark as modified
        key.ModifiedLanguages.Add(language);
        key.IsModified = true;
        
        // Update missing translations status
        key.UpdateMissingTranslationsStatus();
        
        // Mark as having unsaved changes
        HasUnsavedChanges = true;
        
        StatusMessage = $"Accepted suggestion for '{key.Key}' in {LanguageHelper.GetLanguageName(language)}.";
    }

    [RelayCommand]
    private void RejectSuggestion((TranslationKey key, string language) parameters)
    {
        var (key, language) = parameters;
        
        if (!key.SuggestedValues.ContainsKey(language))
            return;

        // Remove the suggestion
        key.SuggestedValues.Remove(language);
        
        // Update missing translations status (in case removing the suggestion makes it missing)
        key.UpdateMissingTranslationsStatus();
        
        // Mark as having unsaved changes
        HasUnsavedChanges = true;
        
        StatusMessage = $"Rejected suggestion for '{key.Key}' in {LanguageHelper.GetLanguageName(language)}.";
    }
}

