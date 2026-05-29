using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
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
    private readonly PathDisplayService _pathDisplayService;
    private string _lastExportFolder = string.Empty;
    private string _lastExportFileName = string.Empty;
    
    // Store minimal display paths for SourceFiles (for grid display)
    private Dictionary<SourceFile, string?> _sourceFileMinimalPaths = new();
    
    /// <summary>
    /// Public accessor for minimal display paths (for grid binding)
    /// </summary>
    public Dictionary<SourceFile, string?> SourceFileMinimalPaths => _sourceFileMinimalPaths;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click Import to load translation files.";

    [ObservableProperty]
    private string _username = "User";

    [ObservableProperty]
    private bool _isDeveloper = false;

    [ObservableProperty]
    private ObservableCollection<SourceFile?> _availableSourceFiles = new();

    [ObservableProperty]
    private SourceFile? _selectedSourceFile;

    [ObservableProperty]
    private ObservableCollection<NamespaceFilterItem> _namespaceFilterItems = new();

    [ObservableProperty]
    private ObservableCollection<FileFilterItem> _fileFilterItems = new();

    [ObservableProperty]
    private string _selectedNamespacesText = "All";

    [ObservableProperty]
    private string _selectedFilesText = "All";

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

    // Track the root directory to prevent importing a second one
    private string? _rootDirectoryPath = null;

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
    [NotifyPropertyChangedFor(nameof(Step6Background), nameof(Step6Foreground), nameof(Step6Status))]
    private StepStatus _deployStepStatus = StepStatus.NotStarted;

    [ObservableProperty]
    private bool _showOriginalValues;

    [ObservableProperty]
    private bool _showOnlyMissingTranslations;

    [ObservableProperty]
    private bool _showOnlyWithSuggestions;

    [ObservableProperty]
    private bool _showOnlyUnconfirmed;

    [ObservableProperty]
    private bool _showFilters = true;

    [ObservableProperty]
    private ObservableCollection<FilePair> _filePairs = new();

    [ObservableProperty]
    private EditMode _currentMode = EditMode.Edit;

    [ObservableProperty]
    private ObservableCollection<ImportError> _importErrors = new();
    
    [ObservableProperty]
    private bool _hasErrors;
    
    [ObservableProperty]
    private int _errorCount;

    public bool ShowImportStep => CurrentStep == WorkflowStep.Import;
    public bool ShowFileMappingStep => CurrentStep == WorkflowStep.FileMapping;
    public bool ShowModeSelectionStep => CurrentStep == WorkflowStep.ModeSelection;
    public bool ShowEditStep => CurrentStep == WorkflowStep.Edit;
    public bool ShowExportStep => CurrentStep == WorkflowStep.Export;
    public bool ShowDeployStep => CurrentStep == WorkflowStep.Deploy;

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

    public SolidColorBrush Step6Background => DeployStepStatus switch
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

    public SolidColorBrush Step6Foreground => DeployStepStatus switch
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

    public string Step6Status => DeployStepStatus switch
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
        _pathDisplayService = new PathDisplayService();

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
            // Clear previous errors
            ImportErrors.Clear();
            
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

            // PHASE 1: Validate ALL files first (all-or-nothing approach)
            var validatedFiles = new List<(IStorageFile file, string filePath, string fileName, string extension, FileType fileType, string language)>();
            
            foreach (var file in files)
            {
                var filePath = file.Path.LocalPath;
                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath).ToLower();

                // Check for duplicate files (case-insensitive)
                if (ImportedFileNames.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)) ||
                    IgnoredFileNames.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.DuplicateFile,
                        FileName = fileName,
                        Message = $"File '{fileName}' has already been imported",
                        Guidance = "This file is already loaded. Remove it from your selection and try again."
                    });
                    continue;
                }

                // Validate naming convention
                var fileType = extension == ".json" ? FileType.Json : FileType.Resx;
                var isValidName = fileType == FileType.Json 
                    ? _jsonReader.ValidateNamingConvention(fileName)
                    : _resxReader.ValidateNamingConvention(fileName);
                
                if (!isValidName)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.InvalidNamingConvention,
                        FileName = fileName,
                        Message = $"File name doesn't match expected pattern",
                        Guidance = fileType == FileType.Json
                            ? "Expected: {BaseName}.{language}.json (e.g., Forms.en.json, Forms.es.json)"
                            : "Expected: {BaseName}.resx or {BaseName}_{language}.resx (e.g., FormTranslations.resx, FormTranslations_es.resx)"
                    });
                    continue;
                }

                // Check if language is supported (en or es only)
                var language = ExtractLanguage(fileName, fileType);
                
                if (string.IsNullOrEmpty(language) || (language != "en" && language != "es"))
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.UnsupportedLanguage,
                        FileName = fileName,
                        Message = $"Language '{language}' is not supported",
                        Guidance = "Only English (en) and Spanish (es) are currently supported."
                    });
                    continue;
                }

                // Validate file can be parsed (without actually importing yet)
                try
                {
                    if (fileType == FileType.Json)
                    {
                        // Try to parse JSON to validate structure
                        var json = File.ReadAllText(filePath);
                        JsonDocument.Parse(json); // Will throw if invalid
                    }
                    else
                    {
                        // Try to load RESX XML to validate structure
                        System.Xml.Linq.XDocument.Load(filePath); // Will throw if invalid
                    }
                }
                catch (JsonException ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.JsonParseError,
                        FileName = fileName,
                        Message = $"Failed to parse JSON: {ex.Message}",
                        Guidance = "Ensure the file contains valid JSON syntax. Check for missing commas, brackets, or quotes."
                    });
                    continue;
                }
                catch (System.Xml.XmlException ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.ResxParseError,
                        FileName = fileName,
                        Message = $"Failed to parse RESX XML: {ex.Message}",
                        Guidance = "Ensure the file is a valid RESX file with proper XML structure."
                    });
                    continue;
                }
                catch (Exception ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.Other,
                        FileName = fileName,
                        Message = $"Unexpected error: {ex.Message}",
                        Guidance = "Please check the file and try again. If the problem persists, contact support."
                    });
                    continue;
                }

                // File passed all validation - add to validated list
                validatedFiles.Add((file, filePath, fileName, extension, fileType, language));
            }

            // Update error state
            HasErrors = ImportErrors.Count > 0;
            ErrorCount = ImportErrors.Count;

            // If ANY errors occurred, don't import anything
            if (HasErrors)
            {
                StatusMessage = $"Import failed with {ErrorCount} error(s). Click 'View Error Details' to see what went wrong. No files were imported.";
                return;
            }

            // If no files passed validation
            if (validatedFiles.Count == 0)
            {
                StatusMessage = "No valid files to import.";
                return;
            }

            // PHASE 2: All files validated successfully - now import them
            var translationFiles = new List<TranslationFile>();

            foreach (var (file, filePath, fileName, extension, fileType, language) in validatedFiles)
            {
                try
                {
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
                    // This shouldn't happen since we validated already, but handle it just in case
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.Other,
                        FileName = fileName,
                        Message = $"Unexpected error during import: {ex.Message}",
                        Guidance = "Please try again. If the problem persists, contact support."
                    });
                }
            }

            // If errors occurred during import phase (shouldn't happen but check anyway)
            if (HasErrors)
            {
                StatusMessage = $"Import failed with {ErrorCount} error(s). Click 'View Error Details' to see what went wrong.";
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
            
            // Success - all files imported
            StatusMessage = $"Successfully imported {translationFiles.Count} file(s). Review the imported files below and click 'Confirm & Continue' to proceed.";
            
            HasKeys = _translationStore.GetAllKeys().Count > 0;
            ImportStepStatus = StepStatus.InProgress;
            LanguagesChanged?.Invoke(this, EventArgs.Empty);

            // Only save progress if import was successful
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

    public async Task ImportDroppedDirectory(IStorageFolder folder)
    {
        try
        {
            // Check if a root directory has already been selected
            if (_rootDirectoryPath != null)
            {
                StatusMessage = "A root directory is already loaded. Please use 'Start Over' to import from a different directory.";
                return;
            }

            var parentPath = folder.Path.LocalPath;
            StatusMessage = $"Scanning directory: {folder.Name}...";

            // Show branch warning dialog first
            var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (window == null)
            {
                StatusMessage = "Unable to show dialog - no main window found.";
                return;
            }

            var branchWarning = new BranchWarningDialog();
            var confirmed = await branchWarning.ShowDialog<bool>(window);
            
            if (!confirmed)
            {
                StatusMessage = "Import cancelled.";
                return;
            }

            // Scan directory using DirectoryScanner
            var scanner = new DirectoryScanner(_jsonReader, _resxReader);
            var scanResults = scanner.ScanDirectory(parentPath);

            if (scanResults.Count == 0)
            {
                StatusMessage = "No subdirectories found in the dropped directory.";
                return;
            }

            // Show directory selector dialog
            var directories = new ObservableCollection<DirectoryScanResult>(scanResults);
            var selectorViewModel = new DirectorySelectorViewModel(parentPath, directories);
            var selectorDialog = new DirectorySelectorDialog
            {
                DataContext = selectorViewModel
            };

            var importConfirmed = await selectorDialog.ShowDialog<bool>(window);
            
            if (!importConfirmed)
            {
                StatusMessage = "Import cancelled.";
                return;
            }

            // Store the root directory path
            _rootDirectoryPath = parentPath;

            // Gather all files from parent directory AND selected subdirectories
            var allFilesToImport = new List<string>();
            
            // First, check parent directory itself for translation files (non-recursive)
            var parentFiles = scanner.FindTranslationFilesInDirectory(parentPath);
            allFilesToImport.AddRange(parentFiles);
            
            // Then, gather files from selected subdirectories (recursive)
            var selectedDirectories = selectorViewModel.Directories.Where(d => d.IsSelected).ToList();
            foreach (var dir in selectedDirectories)
            {
                allFilesToImport.AddRange(dir.TranslationFiles);
            }
            
            // Check if any files were found
            if (allFilesToImport.Count == 0)
            {
                StatusMessage = "No translation files found in the selected directories.";
                _rootDirectoryPath = null; // Reset since nothing was imported
                return;
            }

            StatusMessage = $"Found {allFilesToImport.Count} translation file(s). Importing...";

            // Import all files using existing validation logic
            ImportErrors.Clear();

            var validatedFiles = new List<(string filePath, string fileName, string extension, FileType fileType, string language)>();
            
            foreach (var filePath in allFilesToImport)
            {
                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath).ToLower();
                var fileType = extension == ".json" ? FileType.Json : FileType.Resx;

                // Calculate relative directory path
                var fileDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
                var relativePath = Path.GetRelativePath(parentPath, fileDirectory);
                var directoryPath = relativePath != "." ? relativePath : null;
                
                // Extract base name
                var baseName = ExtractBaseFileName(filePath, fileType);

                // Check for duplicate files (same name, type, AND directory path)
                var isDuplicate = _translationStore.SourceFiles.Any(sf => 
                    string.Equals(sf.Name, baseName, StringComparison.OrdinalIgnoreCase) &&
                    sf.Type == fileType &&
                    sf.DirectoryPath == directoryPath);

                if (isDuplicate)
                {
                    var displayPath = directoryPath != null ? $"{directoryPath}/{fileName}" : fileName;
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.DuplicateFile,
                        FileName = displayPath,
                        Message = $"File already imported from this directory"
                    });
                    continue;
                }

                // Check if language is supported
                var language = ExtractLanguage(fileName, fileType);
                
                if (string.IsNullOrEmpty(language) || (language != "en" && language != "es"))
                {
                    var displayPath = directoryPath != null ? $"{directoryPath}/{fileName}" : fileName;
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.UnsupportedLanguage,
                        FileName = displayPath,
                        Message = $"Unsupported language '{language}'. Only English (en) and Spanish (es) are supported."
                    });
                    continue;
                }

                // Validate the file can be read
                try
                {
                    if (fileType == FileType.Json)
                    {
                        var _ = _jsonReader.ReadFile(filePath);
                    }
                    else
                    {
                        var _ = _resxReader.ReadFile(filePath);
                    }

                    validatedFiles.Add((filePath, fileName, extension, fileType, language));
                }
                catch (Exception ex)
                {
                    var displayPath = directoryPath != null ? $"{directoryPath}/{fileName}" : fileName;
                    var errorType = extension == ".json" ? ImportErrorType.JsonParseError : ImportErrorType.ResxParseError;
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = errorType,
                        FileName = displayPath,
                        Message = $"Failed to parse: {ex.Message}"
                    });
                }
            }

            // Group and import validated files
            var groupedByDirectory = validatedFiles
                .GroupBy(f =>
                {
                    var fileDirectory = Path.GetDirectoryName(f.filePath) ?? string.Empty;
                    var relativePath = Path.GetRelativePath(parentPath, fileDirectory);
                    return relativePath != "." ? relativePath : null;
                })
                .ToList();

            int successCount = 0;
            foreach (var dirGroup in groupedByDirectory)
            {
                var directoryPath = dirGroup.Key;
                
                foreach (var group in dirGroup.GroupBy(f => (ExtractBaseFileName(f.filePath, f.fileType), f.fileType)))
                {
                    var baseName = group.Key.Item1;
                    var fileType = group.Key.Item2;
                    
                    var filesToConsolidate = group.Select(f =>
                    {
                        return fileType == FileType.Json
                            ? _jsonReader.ReadFile(f.filePath)
                            : _resxReader.ReadFile(f.filePath);
                    }).ToList();

                    var consolidated = fileType == FileType.Json
                        ? _jsonReader.ConsolidateKeys(filesToConsolidate)
                        : _resxReader.ConsolidateKeys(filesToConsolidate);

                    // Update source file to include directory path
                    foreach (var key in consolidated)
                    {
                        key.Source = new SourceFile(baseName, fileType, directoryPath);
                    }

                    _translationStore.AddTranslations(consolidated);

                    // Extract and store template from first file
                    var firstFile = filesToConsolidate.First();
                    if (fileType == FileType.Resx)
                    {
                        var template = _resxReader.ExtractTemplate(firstFile.FilePath);
                        _translationStore.SetResxTemplate(baseName, template);
                    }
                    else
                    {
                        var template = _jsonReader.ExtractTemplate(firstFile.FilePath);
                        _translationStore.SetJsonTemplate(baseName, template);
                    }

                    // Add imported file names to the list for display (include directory path)
                    foreach (var file in group)
                    {
                        var displayPath = !string.IsNullOrEmpty(directoryPath)
                            ? $"{directoryPath}/{file.fileName}"
                            : file.fileName;
                        ImportedFileNames.Add(displayPath);
                    }

                    successCount += filesToConsolidate.Count;
                }
            }

            UpdateFileFilters();
            
            if (ImportErrors.Count > 0)
            {
                StatusMessage = $"Imported {successCount} file(s) with {ImportErrors.Count} error(s). Check the errors list.";
            }
            else
            {
                StatusMessage = $"Successfully imported {successCount} file(s) from directory '{folder.Name}'.";
            }
            
            HasKeys = _translationStore.GetAllKeys().Count > 0;
            ImportStepStatus = StepStatus.InProgress;
            LanguagesChanged?.Invoke(this, EventArgs.Empty);

            // Auto-save imported progress
            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing directory: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BulkImportFromDirectory(Window window)
    {
        try
        {
            // Check if a root directory has already been selected
            if (_rootDirectoryPath != null)
            {
                StatusMessage = "A root directory is already loaded. Please use 'Start Over' to import from a different directory.";
                return;
            }

            // Step 1: Show branch warning dialog
            var branchWarning = new BranchWarningDialog();
            var confirmed = await branchWarning.ShowDialog<bool>(window);
            
            if (!confirmed)
            {
                return;
            }

            // Step 2: Open folder picker for parent directory
            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Parent Directory Containing Repositories",
                AllowMultiple = false
            });

            if (folders.Count == 0)
            {
                return;
            }

            var parentPath = folders[0].Path.LocalPath;

            // Step 3: Scan directories using DirectoryScanner
            var scanner = new DirectoryScanner(_jsonReader, _resxReader);
            var scanResults = scanner.ScanDirectory(parentPath);

            if (scanResults.Count == 0)
            {
                StatusMessage = "No subdirectories found in the selected directory.";
                return;
            }

            // Step 4: Show directory selector dialog
            var directories = new ObservableCollection<DirectoryScanResult>(scanResults);
            var selectorViewModel = new DirectorySelectorViewModel(parentPath, directories);
            var selectorDialog = new DirectorySelectorDialog
            {
                DataContext = selectorViewModel
            };

            var importConfirmed = await selectorDialog.ShowDialog<bool>(window);
            
            if (!importConfirmed)
            {
                return;
            }

            // Store the root directory path
            _rootDirectoryPath = parentPath;

            // Step 5: Gather all files from parent directory AND selected subdirectories
            var allFilesToImport = new List<string>();
            
            // First, check parent directory itself for translation files (non-recursive)
            var parentFiles = scanner.FindTranslationFilesInDirectory(parentPath);
            allFilesToImport.AddRange(parentFiles);
            
            // Then, gather files from selected subdirectories (recursive)
            var selectedDirectories = selectorViewModel.Directories.Where(d => d.IsSelected).ToList();
            foreach (var dir in selectedDirectories)
            {
                allFilesToImport.AddRange(dir.TranslationFiles);
            }
            
            // Check if any files were found
            if (allFilesToImport.Count == 0)
            {
                StatusMessage = "No translation files found in the selected directory or its subdirectories.";
                return;
            }

            // Step 6: Import all files using existing validation logic
            // Clear previous errors
            ImportErrors.Clear();

            var validatedFiles = new List<(string filePath, string fileName, string extension, FileType fileType, string language)>();
            
            foreach (var filePath in allFilesToImport)
            {
                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath).ToLower();
                var fileType = extension == ".json" ? FileType.Json : FileType.Resx;

                // Calculate relative directory path for duplicate checking
                var fileDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
                var relativePath = Path.GetRelativePath(parentPath, fileDirectory);
                var directoryPath = relativePath != "." ? relativePath : null;
                
                // Extract base name for duplicate checking
                var baseName = ExtractBaseFileName(filePath, fileType);

                // Check for duplicate files (same name, type, AND directory path)
                var isDuplicate = _translationStore.SourceFiles.Any(sf => 
                    string.Equals(sf.Name, baseName, StringComparison.OrdinalIgnoreCase) &&
                    sf.Type == fileType &&
                    sf.DirectoryPath == directoryPath);

                if (isDuplicate)
                {
                    var displayPath = directoryPath != null ? $"{directoryPath}/{fileName}" : fileName;
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.DuplicateFile,
                        FileName = fileName,
                        Message = $"File '{displayPath}' has already been imported",
                        Guidance = "This file is already loaded. Remove it from your selection and try again."
                    });
                    continue;
                }

                // Validate naming convention
                var isValidName = fileType == FileType.Json 
                    ? _jsonReader.ValidateNamingConvention(fileName)
                    : _resxReader.ValidateNamingConvention(fileName);
                
                if (!isValidName)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.InvalidNamingConvention,
                        FileName = fileName,
                        Message = $"File name doesn't match expected pattern",
                        Guidance = fileType == FileType.Json
                            ? "Expected: {BaseName}.{language}.json (e.g., Forms.en.json, Forms.es.json)"
                            : "Expected: {BaseName}.resx or {BaseName}_{language}.resx (e.g., FormTranslations.resx, FormTranslations_es.resx)"
                    });
                    continue;
                }

                // Check if language is supported (en or es only)
                var language = ExtractLanguage(fileName, fileType);
                
                if (string.IsNullOrEmpty(language) || (language != "en" && language != "es"))
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.UnsupportedLanguage,
                        FileName = fileName,
                        Message = $"Language '{language}' is not supported",
                        Guidance = "Only English (en) and Spanish (es) are currently supported."
                    });
                    continue;
                }

                // Validate file can be parsed
                try
                {
                    if (fileType == FileType.Json)
                    {
                        var json = File.ReadAllText(filePath);
                        JsonDocument.Parse(json);
                    }
                    else
                    {
                        System.Xml.Linq.XDocument.Load(filePath);
                    }
                }
                catch (JsonException ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.JsonParseError,
                        FileName = fileName,
                        Message = $"Failed to parse JSON: {ex.Message}",
                        Guidance = "Ensure the file contains valid JSON syntax. Check for missing commas, brackets, or quotes."
                    });
                    continue;
                }
                catch (System.Xml.XmlException ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.ResxParseError,
                        FileName = fileName,
                        Message = $"Failed to parse RESX XML: {ex.Message}",
                        Guidance = "Ensure the file is a valid RESX file with proper XML structure."
                    });
                    continue;
                }
                catch (Exception ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.Other,
                        FileName = fileName,
                        Message = $"Unexpected error: {ex.Message}",
                        Guidance = "Please check the file and try again. If the problem persists, contact support."
                    });
                    continue;
                }

                // File passed all validation
                validatedFiles.Add((filePath, fileName, extension, fileType, language));
            }

            // Update error state
            HasErrors = ImportErrors.Count > 0;
            ErrorCount = ImportErrors.Count;

            // If ALL files failed validation, don't import anything
            if (validatedFiles.Count == 0 && allFilesToImport.Count > 0)
            {
                StatusMessage = $"Bulk import failed with {ErrorCount} error(s). Click 'View Error Details' to see what went wrong. No files were imported.";
                return;
            }

            // Import validated files
            var translationFiles = new List<TranslationFile>();

            foreach (var (filePath, fileName, extension, fileType, language) in validatedFiles)
            {
                try
                {
                    TranslationFile translationFile = extension switch
                    {
                        ".json" => _jsonReader.ReadFile(filePath),
                        ".resx" => _resxReader.ReadFile(filePath),
                        _ => throw new NotSupportedException($"Unsupported file type: {extension}")
                    };

                    // Calculate relative directory path from parent directory
                    var fileDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
                    var relativePath = Path.GetRelativePath(parentPath, fileDirectory);
                    
                    // Only set if not "." (meaning the file is in a subdirectory, not the parent itself)
                    translationFile.RelativeDirectoryPath = relativePath != "." ? relativePath : null;

                    translationFiles.Add(translationFile);
                    
                    // Add to imported file names with directory path if applicable
                    var displayPath = translationFile.RelativeDirectoryPath != null
                        ? $"{translationFile.RelativeDirectoryPath}/{fileName}"
                        : fileName;
                    ImportedFileNames.Add(displayPath);
                }
                catch (Exception ex)
                {
                    ImportErrors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.Other,
                        FileName = fileName,
                        Message = $"Unexpected error during import: {ex.Message}",
                        Guidance = "Please try again. If the problem persists, contact support."
                    });
                }
            }

            // If errors occurred during import phase
            if (HasErrors)
            {
                StatusMessage = $"Bulk import completed with {ErrorCount} error(s). {translationFiles.Count} file(s) imported successfully. Click 'View Error Details' to see errors.";
            }

            // Group files by base name, file type, AND directory path to keep files from different directories separate
            var groupedFiles = translationFiles
                .GroupBy(tf => (ExtractBaseFileName(tf.FilePath, tf.FileType), tf.FileType, tf.RelativeDirectoryPath ?? string.Empty))
                .ToList();

            foreach (var group in groupedFiles)
            {
                var consolidated = group.Key.FileType == FileType.Json
                    ? _jsonReader.ConsolidateKeys(group.ToList())
                    : _resxReader.ConsolidateKeys(group.ToList());

                _translationStore.AddTranslations(consolidated);

                // Extract and store templates
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
            
            if (!HasErrors)
            {
                StatusMessage = $"Successfully bulk imported {translationFiles.Count} file(s) from {selectedDirectories.Count} repositor{(selectedDirectories.Count == 1 ? "y" : "ies")}. Review the imported files and click 'Confirm & Continue'.";
            }
            
            HasKeys = _translationStore.GetAllKeys().Count > 0;
            ImportStepStatus = StepStatus.InProgress;
            LanguagesChanged?.Invoke(this, EventArgs.Empty);

            SaveProgress();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during bulk import: {ex.Message}";
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

        // Calculate minimal display paths for all source files
        _sourceFileMinimalPaths = _pathDisplayService.CalculateMinimalPaths(
            _translationStore.SourceFiles,
            sf => sf.Name,
            sf => sf.DirectoryPath
        );

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

        // Update namespace filter items
        UpdateNamespaceFilterItems();
        
        // Update file filter items
        UpdateFileFilterItems();
    }

    private void UpdateNamespaceFilterItems()
    {
        // Unsubscribe from old items
        foreach (var item in NamespaceFilterItems)
        {
            item.SelectionChanged -= OnNamespaceFilterItemSelectionChanged;
        }

        NamespaceFilterItems.Clear();

        // Get unique namespaces (top-level directory)
        var namespaces = _translationStore.SourceFiles
            .Select(sf => GetTopLevelDirectory(sf.DirectoryPath))
            .Distinct()
            .OrderBy(ns => ns)
            .ToList();

        // Add "All Namespaces" option
        var allItem = new NamespaceFilterItem
        {
            Namespace = string.Empty,
            IsSelected = true,
            FileCount = _translationStore.SourceFiles.Count
        };
        allItem.SelectionChanged += OnNamespaceFilterItemSelectionChanged;
        NamespaceFilterItems.Add(allItem);

        // Add individual namespaces
        foreach (var ns in namespaces)
        {
            var fileCount = _translationStore.SourceFiles.Count(sf => GetTopLevelDirectory(sf.DirectoryPath) == ns);
            var item = new NamespaceFilterItem
            {
                Namespace = ns,
                IsSelected = false,
                FileCount = fileCount
            };
            item.SelectionChanged += OnNamespaceFilterItemSelectionChanged;
            NamespaceFilterItems.Add(item);
        }

        // Update display text
        UpdateNamespacesDisplayText();
    }

    private void UpdateFileFilterItems()
    {
        // Unsubscribe from old items
        foreach (var item in FileFilterItems)
        {
            item.SelectionChanged -= OnFileFilterItemSelectionChanged;
        }

        FileFilterItems.Clear();

        // Get selected namespaces
        var selectedNamespaces = NamespaceFilterItems
            .Where(n => n.IsSelected && n.Namespace != string.Empty)
            .Select(n => n.Namespace)
            .ToList();

        // If "All Namespaces" is selected or no specific namespaces, show all files
        var allNamespacesSelected = NamespaceFilterItems.FirstOrDefault()?.IsSelected == true;
        
        IEnumerable<SourceFile> filesToShow;
        if (allNamespacesSelected || selectedNamespaces.Count == 0)
        {
            filesToShow = _translationStore.SourceFiles;
        }
        else
        {
            filesToShow = _translationStore.SourceFiles
                .Where(sf => selectedNamespaces.Contains(GetTopLevelDirectory(sf.DirectoryPath)));
        }

        // Add "All Files" option
        var allFilesItem = new FileFilterItem
        {
            Source = new SourceFile("All Files", FileType.Json, null),
            IsSelected = true
        };
        allFilesItem.SelectionChanged += OnFileFilterItemSelectionChanged;
        FileFilterItems.Add(allFilesItem);

        // Add individual files
        foreach (var sourceFile in filesToShow.OrderBy(f => f.Name).ThenBy(f => f.Type).ThenBy(f => f.DirectoryPath))
        {
            var item = new FileFilterItem
            {
                Source = sourceFile,
                IsSelected = false
            };
            item.SelectionChanged += OnFileFilterItemSelectionChanged;
            FileFilterItems.Add(item);
        }

        // Update display text
        UpdateFilesDisplayText();
    }

    private string GetTopLevelDirectory(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            return string.Empty;

        var parts = directoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private void OnNamespaceFilterItemSelectionChanged(object? sender, bool isSelected)
    {
        if (sender is not NamespaceFilterItem item)
            return;

        // If this is "All Namespaces", deselect all others
        if (item.Namespace == string.Empty && isSelected)
        {
            foreach (var other in NamespaceFilterItems.Where(n => n != item))
            {
                other.IsSelected = false;
            }
        }
        // If a specific namespace is selected, deselect "All Namespaces"
        else if (item.Namespace != string.Empty && isSelected)
        {
            var allItem = NamespaceFilterItems.FirstOrDefault();
            if (allItem != null)
            {
                allItem.IsSelected = false;
            }
        }

        // If no namespaces are selected, select "All Namespaces"
        if (!NamespaceFilterItems.Any(n => n.IsSelected))
        {
            var allItem = NamespaceFilterItems.FirstOrDefault();
            if (allItem != null)
            {
                allItem.IsSelected = true;
            }
        }

        // Update file filter items based on selected namespaces
        UpdateFileFilterItems();

        // Update display text
        UpdateNamespacesDisplayText();

        // Apply filters
        ApplyFileFilters();
    }

    private void OnFileFilterItemSelectionChanged(object? sender, bool isSelected)
    {
        if (sender is not FileFilterItem item)
            return;

        // If this is "All Files", deselect all others
        if (item.Source.Name == "All Files" && isSelected)
        {
            foreach (var other in FileFilterItems.Where(f => f != item))
            {
                other.IsSelected = false;
            }
        }
        // If a specific file is selected, deselect "All Files"
        else if (item.Source.Name != "All Files" && isSelected)
        {
            var allItem = FileFilterItems.FirstOrDefault();
            if (allItem != null)
            {
                allItem.IsSelected = false;
            }
        }

        // If no files are selected, select "All Files"
        if (!FileFilterItems.Any(f => f.IsSelected))
        {
            var allItem = FileFilterItems.FirstOrDefault();
            if (allItem != null)
            {
                allItem.IsSelected = true;
            }
        }

        // Update display text
        UpdateFilesDisplayText();

        // Apply filters
        ApplyFileFilters();
    }

    private void UpdateNamespacesDisplayText()
    {
        var allItem = NamespaceFilterItems.FirstOrDefault();
        if (allItem?.IsSelected == true)
        {
            SelectedNamespacesText = "All Namespaces";
            return;
        }

        var selected = NamespaceFilterItems.Where(n => n.IsSelected && n.Namespace != string.Empty).ToList();
        if (selected.Count == 0)
        {
            SelectedNamespacesText = "All Namespaces";
        }
        else if (selected.Count == 1)
        {
            SelectedNamespacesText = selected[0].Namespace;
        }
        else
        {
            SelectedNamespacesText = $"{selected.Count} namespaces";
        }
    }

    private void UpdateFilesDisplayText()
    {
        var allItem = FileFilterItems.FirstOrDefault();
        if (allItem?.IsSelected == true)
        {
            SelectedFilesText = "All Files";
            return;
        }

        var selected = FileFilterItems.Where(f => f.IsSelected && f.Source.Name != "All Files").ToList();
        if (selected.Count == 0)
        {
            SelectedFilesText = "All Files";
        }
        else if (selected.Count == 1)
        {
            SelectedFilesText = selected[0].DisplayName;
        }
        else
        {
            SelectedFilesText = $"{selected.Count} files";
        }
    }

    private void ApplyFileFilters()
    {
        // Get selected namespaces
        var allNamespacesSelected = NamespaceFilterItems.FirstOrDefault()?.IsSelected == true;
        var selectedNamespaces = NamespaceFilterItems
            .Where(n => n.IsSelected && n.Namespace != string.Empty)
            .Select(n => n.Namespace)
            .ToList();

        // Get selected files
        var selectedFiles = FileFilterItems
            .Where(f => f.IsSelected && f.Source.Name != "All Files")
            .Select(f => f.Source)
            .ToList();

        var allFilesSelected = FileFilterItems.FirstOrDefault()?.IsSelected == true;
        
        // Priority 1: If specific files are selected, use those
        if (selectedFiles.Count > 0)
        {
            _translationStore.FilterBySourceFiles(selectedFiles);
        }
        // Priority 2: If specific namespaces are selected (but all files within those namespaces), filter by namespace
        else if (!allNamespacesSelected && selectedNamespaces.Count > 0)
        {
            var filesInNamespaces = _translationStore.SourceFiles
                .Where(sf => selectedNamespaces.Contains(GetTopLevelDirectory(sf.DirectoryPath)))
                .ToList();
            _translationStore.FilterBySourceFiles(filesInNamespaces);
        }
        // Priority 3: Show all files
        else
        {
            _translationStore.FilterBySourceFiles(null!);
        }

        UpdateStatusMessage();
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

    /// <summary>
    /// Gets the minimal display path for a source file (for use in converters)
    /// </summary>
    public string? GetMinimalDisplayPath(SourceFile sourceFile)
    {
        return _sourceFileMinimalPaths.TryGetValue(sourceFile, out var path) ? path : null;
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SelectedSourceFile = null;
        SearchText = string.Empty;
        ShowOriginalValues = false;
        ShowOnlyMissingTranslations = false;
        ShowOnlyWithSuggestions = false;
        ShowOnlyUnconfirmed = false;
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

    partial void OnShowOnlyWithSuggestionsChanged(bool value)
    {
        _translationStore.FilterBySuggestions(value);
        UpdateStatusMessage();
    }

    partial void OnShowOnlyUnconfirmedChanged(bool value)
    {
        _translationStore.FilterByConfirmation(value);
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
            _jsonWriter.WriteFiles(jsonKeys, outputPath, sourceFileName => _translationStore.GetJsonTemplate(sourceFileName), Username, CurrentMode == EditMode.Edit);
        }

        if (resxKeys.Count > 0)
        {
            // Provide template provider function to preserve original RESX structure
            _resxWriter.WriteFiles(resxKeys, outputPath, sourceFileName => _translationStore.GetResxTemplate(sourceFileName), Username, CurrentMode == EditMode.Edit);
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

        // Group keys by source file (including directory path)
        var groupedByFile = allKeys.GroupBy(k => k.Source);

        foreach (var fileGroup in groupedByFile)
        {
            var source = fileGroup.Key;
            var fileKeys = fileGroup.ToList();

            // Get all languages for this file
            var languages = fileKeys
                .SelectMany(k => k.LanguageValues.Keys)
                .Distinct()
                .ToList();

            // Check each language file
            foreach (var language in languages)
            {
                string fileName;
                if (source.Type == FileType.Json)
                {
                    fileName = $"{source.Name}.{language}.json";
                }
                else // RESX
                {
                    fileName = language == "en" 
                        ? $"{source.Name}.resx" 
                        : $"{source.Name}_{language}.resx";
                }

                // Include directory path in the comparison
                var fullPath = !string.IsNullOrEmpty(source.DirectoryPath)
                    ? $"{source.DirectoryPath}/{fileName}"
                    : fileName;

                if (!importedFileNamesLower.Contains(fullPath.ToLower()))
                {
                    newFiles.Add(fullPath);
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

    /// <summary>
    /// Calculates minimal unique directory paths for display in UI
    /// Returns null for files with no duplicates, otherwise returns the shortest path segment that makes them unique
    /// </summary>
    private Dictionary<SourceFile, string?> CalculateMinimalDisplayPaths(IEnumerable<SourceFile> sourceFiles)
    {
        var result = new Dictionary<SourceFile, string?>();
        var filesByName = sourceFiles.GroupBy(sf => sf.Name).ToList();

        foreach (var group in filesByName)
        {
            var filesWithPaths = group.Where(sf => sf.DirectoryPath != null).ToList();
            var filesWithoutPaths = group.Where(sf => sf.DirectoryPath == null).ToList();

            // If there's only one file with this name (or all have no directory path), no need to show path
            if (group.Count() == 1 || filesWithPaths.Count == 0)
            {
                foreach (var file in group)
                {
                    result[file] = null;
                }
                continue;
            }

            // If we have duplicates, calculate minimal unique paths
            foreach (var file in filesWithPaths)
            {
                if (file.DirectoryPath == null)
                {
                    result[file] = null;
                    continue;
                }

                // Split the directory path into segments
                var segments = file.DirectoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Find the minimal unique suffix by comparing with other files with the same name
                var otherFiles = filesWithPaths.Where(f => f != file).ToList();
                var minimalPath = file.DirectoryPath;

                // Try increasingly shorter paths (from end) until we find one that's unique
                for (int i = segments.Length - 1; i >= 0; i--)
                {
                    var candidatePath = string.Join("/", segments.Skip(i));
                    
                    // Check if this path is unique among files with the same name
                    var isUnique = !otherFiles.Any(other =>
                    {
                        if (other.DirectoryPath == null) return false;
                        var otherSegments = other.DirectoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        var otherCandidate = string.Join("/", otherSegments.Skip(Math.Max(0, otherSegments.Length - segments.Length + i)));
                        return string.Equals(candidatePath, otherCandidate, StringComparison.OrdinalIgnoreCase);
                    });

                    if (isUnique)
                    {
                        minimalPath = candidatePath;
                    }
                    else
                    {
                        break; // Need more segments to be unique
                    }
                }

                result[file] = minimalPath;
            }

            // Files without directory paths in a group with duplicates
            foreach (var file in filesWithoutPaths)
            {
                result[file] = null; // Or could show "(root)" or similar
            }
        }

        return result;
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
            HasUnsavedChanges = false;
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
        _rootDirectoryPath = null; // Reset root directory

        // Reset workflow state
        CurrentStep = WorkflowStep.Import;
        ImportStepStatus = StepStatus.InProgress;
        FileMappingStepStatus = StepStatus.NotStarted;
        ModeSelectionStepStatus = StepStatus.NotStarted;
        EditStepStatus = StepStatus.NotStarted;
        ExportStepStatus = StepStatus.NotStarted;
        CurrentMode = EditMode.Edit;

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

            // Check for unresolved suggestions (only in Edit mode)
            // In Suggest mode, having suggestions is the whole point - don't warn about them
            if (CurrentMode == EditMode.Edit)
            {
                var keysWithSuggestions = _translationStore.GetAllKeys()
                    .Where(k => k.HasAnySuggestions)
                    .ToList();

                if (keysWithSuggestions.Count > 0)
                {
                    var totalSuggestions = keysWithSuggestions.Sum(k => k.SuggestedValues.Count);
                    var message = $"There are {totalSuggestions} unresolved suggestion(s) across {keysWithSuggestions.Count} key(s).\n\n" +
                                  "These suggestions have not been accepted or rejected.\n\n" +
                                  "Do you want to stay and review the suggestions, or continue to export anyway?";

                    var dialog = new ConfirmationDialog(
                        message,
                        title: "Confirm Export",
                        header: "⚠️ Unresolved Suggestions Detected");
                    var result = await dialog.ShowDialog<bool?>(window);

                    if (result != true)
                    {
                        StatusMessage = "Review suggestions before exporting.";
                        return;
                    }
                }
            }

            // Check for missing translations (mode-aware)
            // Edit Mode: Missing = no actual value for en/es (suggestions don't count)
            // Suggest Mode: Missing = no value AND no suggestion for en/es
            var keysWithMissingTranslations = _translationStore.GetAllKeys()
                .Where(k =>
                {
                    if (CurrentMode == EditMode.Edit)
                    {
                        // In Edit mode, only check if actual values exist (ignore suggestions)
                        var hasEnglishValue = k.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
                        var hasSpanishValue = k.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
                        return !hasEnglishValue || !hasSpanishValue;
                    }
                    else
                    {
                        // In Suggest mode, check if value OR suggestion exists
                        var hasEnglish = (k.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue)) 
                                        || k.SuggestedValues.ContainsKey("en");
                        var hasSpanish = (k.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue))
                                        || k.SuggestedValues.ContainsKey("es");
                        return !hasEnglish || !hasSpanish;
                    }
                })
                .ToList();

            if (keysWithMissingTranslations.Count > 0)
            {
                var message = $"There are {keysWithMissingTranslations.Count} translation key(s) with missing terms.\n\n" +
                              "Do you want to stay and add the missing translations, or continue to export anyway?";

                var dialog = new ConfirmationDialog(
                    message,
                    title: "Confirm Export",
                    header: "⚠️ Missing Translations Detected");
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

        // Get the main window for the folder picker
        var window = App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (window == null)
        {
            StatusMessage = "Unable to show folder picker.";
            return;
        }

        // Load last export directory from settings, or use default
        var settings = _settingsService.Load();
        var defaultExportPath = !string.IsNullOrEmpty(settings?.LastExportDirectory) && Directory.Exists(settings.LastExportDirectory)
            ? settings.LastExportDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Show folder picker for export destination
        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Export Destination",
            AllowMultiple = false,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(defaultExportPath)
        });

        if (folders.Count == 0)
        {
            StatusMessage = "Export cancelled.";
            return;
        }

        var outputPath = folders[0].Path.LocalPath;

        // Save the selected directory to settings for next time
        if (settings == null)
        {
            settings = new UserSettings { Username = Username };
        }
        settings.LastExportDirectory = outputPath;
        _settingsService.Save(settings);

        // Determine zip file name based on root directory
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string zipFileName;
        
        if (!string.IsNullOrEmpty(_rootDirectoryPath))
        {
            // Use the root directory name in the zip filename
            var rootDirName = Path.GetFileName(_rootDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            zipFileName = $"{rootDirName}_exported_{timestamp}.zip";
        }
        else
        {
            // Fallback to generic name if no root directory
            zipFileName = $"exported_translations_{timestamp}.zip";
        }

        var tempFolderPath = Path.Combine(Path.GetTempPath(), $"isometrix_lingo_export_{timestamp}");
        var zipFilePath = Path.Combine(outputPath, zipFileName);

        try
        {
            // Create temporary folder for exported files
            Directory.CreateDirectory(tempFolderPath);

            // Group all keys by file type and export to temp folder
            var jsonKeys = allKeys.Where(k => k.Source.Type == FileType.Json).ToList();
            var resxKeys = allKeys.Where(k => k.Source.Type == FileType.Resx).ToList();

            if (jsonKeys.Count > 0)
            {
                _jsonWriter.WriteFiles(jsonKeys, tempFolderPath, sourceFileName => _translationStore.GetJsonTemplate(sourceFileName), Username, CurrentMode == EditMode.Edit);
            }

            if (resxKeys.Count > 0)
            {
                _resxWriter.WriteFiles(resxKeys, tempFolderPath, sourceFileName => _translationStore.GetResxTemplate(sourceFileName), Username, CurrentMode == EditMode.Edit);
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
            _lastExportFileName = zipFileName;

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
            // DEBUG: Count confirmations before adding to store
            var confirmationsLoaded = sessionState.TranslationKeys.Count(k => k.ConfirmedBy != null);
            System.Diagnostics.Debug.WriteLine($"[LoadProgress] Loaded {confirmationsLoaded} keys with confirmations out of {sessionState.TranslationKeys.Count} total keys");
            
            // DEBUG: Show some details
            foreach (var key in sessionState.TranslationKeys.Where(k => k.ConfirmedBy != null).Take(3))
            {
                System.Diagnostics.Debug.WriteLine($"  Key: {key.Key}, ConfirmationDisplayText: '{key.ConfirmationDisplayText}'");
            }
            
            // Restore translation keys
            _translationStore.AddTranslations(sessionState.TranslationKeys);
            
            // DEBUG: Count confirmations after adding to store
            var confirmationsInStore = _translationStore.GetAllKeys().Count(k => k.ConfirmedBy != null);
            System.Diagnostics.Debug.WriteLine($"[LoadProgress] Store now has {confirmationsInStore} keys with confirmations");

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

            StatusMessage = $"Loaded {sessionState.TranslationKeys.Count} translation keys from saved progress ({confirmationsLoaded} confirmed).";
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
        if (settings != null)
        {
            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                Username = settings.Username;
            }
            IsDeveloper = settings.IsDeveloper;
        }
    }

    private void DetectFilePairs()
    {
        FilePairs.Clear();

        // Group source files by base name, file type, AND directory path
        var fileGroups = _translationStore.SourceFiles
            .GroupBy(sourceFile => new 
            { 
                BaseName = sourceFile.Name, 
                FileType = sourceFile.Type,
                DirectoryPath = sourceFile.DirectoryPath ?? string.Empty
            })
            .ToList();

        foreach (var group in fileGroups)
        {
            var pair = new FilePair
            {
                BaseName = group.Key.BaseName,
                FileType = group.Key.FileType,
                DirectoryPath = string.IsNullOrEmpty(group.Key.DirectoryPath) ? null : group.Key.DirectoryPath
            };

            // Check which language files exist in this group
            // Note: Each SourceFile represents a base file, not individual language files
            // We need to check if the translation keys have both languages
            var keysForThisSource = _translationStore.GetAllKeys()
                .Where(k => k.Source.Name == group.Key.BaseName && 
                           k.Source.Type == group.Key.FileType &&
                           k.Source.DirectoryPath == (string.IsNullOrEmpty(group.Key.DirectoryPath) ? null : group.Key.DirectoryPath))
                .ToList();

            if (keysForThisSource.Any())
            {
                var hasEnglish = keysForThisSource.Any(k => k.LanguageValues.ContainsKey("en"));
                var hasSpanish = keysForThisSource.Any(k => k.LanguageValues.ContainsKey("es"));

                pair.HasEnglishFile = hasEnglish;
                pair.HasSpanishFile = hasSpanish;
                
                // Set file names for display
                if (hasEnglish)
                {
                    pair.EnglishFileName = group.Key.FileType == FileType.Json 
                        ? $"{group.Key.BaseName}.en.json"
                        : $"{group.Key.BaseName}.resx";
                }
                
                if (hasSpanish)
                {
                    pair.SpanishFileName = group.Key.FileType == FileType.Json
                        ? $"{group.Key.BaseName}.es.json"
                        : $"{group.Key.BaseName}_es.resx";
                }
            }

            FilePairs.Add(pair);
        }

        // Calculate minimal display paths for pairs with duplicate names
        CalculateMinimalFilePairPaths();
    }

    private void CalculateMinimalFilePairPaths()
    {
        // Use PathDisplayService to calculate minimal paths for all file pairs
        var minimalPaths = _pathDisplayService.CalculateMinimalPaths(
            FilePairs,
            pair => pair.BaseName,
            pair => pair.DirectoryPath
        );

        // Assign the calculated minimal paths
        foreach (var pair in FilePairs)
        {
            pair.MinimalDisplayPath = minimalPaths.TryGetValue(pair, out var path) ? path : null;
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
        var modeText = mode == EditMode.Edit ? "Edit" : "Suggest";
        StatusMessage = $"{modeText} mode selected. Click Next to continue.";
    }

    [RelayCommand]
    private void ConfirmModeSelection()
    {
        ModeSelectionStepStatus = StepStatus.Completed;

        if (CurrentMode == EditMode.Deployment)
        {
            // Deployment mode: skip Edit step and go directly to Export
            EditStepStatus = StepStatus.NotStarted; // Skipped
            ExportStepStatus = StepStatus.InProgress;
            CurrentStep = WorkflowStep.Export;
            StatusMessage = "Deployment mode selected. Click 'Export as ZIP' to prepare translations for deployment.";
        }
        else
        {
            // Edit or Suggest mode: proceed to Edit step
            EditStepStatus = StepStatus.InProgress;
            CurrentStep = WorkflowStep.Edit;
            
            var modeText = CurrentMode == EditMode.Edit ? "Edit" : "Suggest";
            StatusMessage = $"{modeText} mode selected. You can now {modeText.ToLower()} translations.";
        }

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

        // Get all keys from the existing file (must match base name, type, AND directory path)
        var existingKeys = _translationStore.GetAllKeys()
            .Where(k => k.Source.Name == pair.BaseName && 
                       k.Source.Type == pair.FileType &&
                       k.Source.DirectoryPath == pair.DirectoryPath)
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

        // Add to imported file names (include directory path)
        var displayPath = !string.IsNullOrEmpty(pair.DirectoryPath)
            ? $"{pair.DirectoryPath}/{newFileName}"
            : newFileName;
        if (!ImportedFileNames.Any(f => string.Equals(f, displayPath, StringComparison.OrdinalIgnoreCase)))
        {
            ImportedFileNames.Add(displayPath);
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
            Height = 230,
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

        // Developer mode checkbox
        var developerCheckBox = new CheckBox
        {
            Content = "Developer Mode (enables deployment features)",
            IsChecked = IsDeveloper,
            Margin = new Thickness(0, 10, 0, 0)
        };
        contentPanel.Children.Add(developerCheckBox);

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

        // Enable save button only when username or developer mode changes
        usernameBox.TextChanged += (s, e) =>
        {
            var newText = usernameBox.Text?.Trim() ?? "";
            var newDeveloper = developerCheckBox.IsChecked == true;
            saveButton.IsEnabled = (!string.IsNullOrWhiteSpace(newText) && newText != Username) || newDeveloper != IsDeveloper;
        };

        developerCheckBox.IsCheckedChanged += (s, e) =>
        {
            var newText = usernameBox.Text?.Trim() ?? "";
            var newDeveloper = developerCheckBox.IsChecked == true;
            saveButton.IsEnabled = (!string.IsNullOrWhiteSpace(newText) && newText != Username) || newDeveloper != IsDeveloper;
        };

        saveButton.Click += (s, args) =>
        {
            var newUsername = usernameBox.Text?.Trim();
            var newDeveloper = developerCheckBox.IsChecked == true;
            bool changed = false;

            if (!string.IsNullOrWhiteSpace(newUsername) && newUsername != Username)
            {
                Username = newUsername;
                changed = true;
            }

            if (newDeveloper != IsDeveloper)
            {
                IsDeveloper = newDeveloper;
                changed = true;
            }

            if (changed)
            {
                var settings = _settingsService.Load() ?? new UserSettings();
                settings.Username = Username;
                settings.IsDeveloper = IsDeveloper;
                settings.LastExportDirectory = settings.LastExportDirectory; // Preserve existing
                settings.LastDeploymentRoot = settings.LastDeploymentRoot; // Preserve existing
                _settingsService.Save(settings);
                StatusMessage = "Profile updated successfully.";
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
    private async Task ShowErrorDetails(Window window)
    {
        if (!HasErrors)
            return;

        var viewModel = new ErrorDetailsViewModel(ImportErrors);
        var dialog = new ErrorDetailsDialog
        {
            DataContext = viewModel
        };

        await dialog.ShowDialog(window);
    }

    [RelayCommand]
    private void AcceptSuggestion((TranslationKey key, string language) parameters)
    {
        var (key, language) = parameters;
        
        // Use the TranslationKey's method to accept the suggestion (handles all state updates and notifications)
        var acceptedValue = key.AcceptSuggestionForLanguage(language);
        
        if (acceptedValue == null)
            return;
        
        // Mark as having unsaved changes
        HasUnsavedChanges = true;
        
        StatusMessage = $"Accepted suggestion for '{key.Key}' in {LanguageHelper.GetLanguageName(language)}.";
    }

    [RelayCommand]
    private void RejectSuggestion((TranslationKey key, string language) parameters)
    {
        var (key, language) = parameters;
        
        // Use the TranslationKey's method to reject the suggestion (handles all state updates and notifications)
        var wasRejected = key.RejectSuggestionForLanguage(language);
        
        if (!wasRejected)
            return;
        
        // Mark as having unsaved changes
        HasUnsavedChanges = true;
        
        StatusMessage = $"Rejected suggestion for '{key.Key}' in {LanguageHelper.GetLanguageName(language)}.";
    }
}

