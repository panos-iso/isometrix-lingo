using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslationManagementTool.Models;
using TranslationManagementTool.Services;
using TranslationManagementTool.Views;

namespace TranslationManagementTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TranslationStore _translationStore;
    private readonly JsonTranslationFileReader _jsonReader;
    private readonly ResxTranslationFileReader _resxReader;
    private readonly JsonTranslationFileWriter _jsonWriter;
    private readonly ResxTranslationFileWriter _resxWriter;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click Import to load translation files.";

    [ObservableProperty]
    private ObservableCollection<FileFilterItem> _fileFilters = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _hasKeys;

    public event EventHandler? LanguagesChanged;

    public MainWindowViewModel()
    {
        _translationStore = new TranslationStore();
        _jsonReader = new JsonTranslationFileReader();
        _resxReader = new ResxTranslationFileReader();
        _jsonWriter = new JsonTranslationFileWriter();
        _resxWriter = new ResxTranslationFileWriter();
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
        
        foreach (var file in files)
        {
            try
            {
                var filePath = file.Path.LocalPath;
                var extension = Path.GetExtension(filePath).ToLower();
                
                TranslationFile translationFile = extension switch
                {
                    ".json" => _jsonReader.ReadFile(filePath),
                    ".resx" => _resxReader.ReadFile(filePath),
                    _ => throw new NotSupportedException($"Unsupported file type: {extension}")
                };
                
                translationFiles.Add(translationFile);
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
        }

        UpdateFileFilters();
        StatusMessage = $"Imported {files.Count} file(s) with {_translationStore.FilteredKeys.Count} translation keys.";
        HasKeys = _translationStore.GetAllKeys().Count > 0;
        LanguagesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ToggleFileFilter(FileFilterItem filter)
    {
        filter.IsSelected = !filter.IsSelected;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task AddKey(Window window)
    {
        var addKeyViewModel = new AddKeyViewModel(
            _translationStore.SourceFiles,
            _translationStore.Languages
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
        }
    }

    private void UpdateFileFilters()
    {
        FileFilters.Clear();
        foreach (var sourceFile in _translationStore.SourceFiles.OrderBy(f => f.Name).ThenBy(f => f.Type))
        {
            var filterItem = new FileFilterItem { Source = sourceFile, IsSelected = true };
            FileFilters.Add(filterItem);
        }
    }

    private void ApplyFilters()
    {
        var selectedFiles = FileFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Source)
            .ToList();

        if (selectedFiles.Count == 0)
        {
            // If nothing selected, show all
            _translationStore.FilterBySourceFiles(null!);
        }
        else
        {
            _translationStore.FilterBySourceFiles(selectedFiles);
        }

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
        else
        {
            var selectedFileCount = FileFilters.Count(f => f.IsSelected);
            if (selectedFileCount == 0 || selectedFileCount == FileFilters.Count)
            {
                StatusMessage = $"Showing {filteredCount} translation keys.";
            }
            else
            {
                StatusMessage = $"Showing {filteredCount} translation keys from {selectedFileCount} file(s).";
            }
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
            _jsonWriter.WriteFiles(jsonKeys, outputPath);
        }

        if (resxKeys.Count > 0)
        {
            _resxWriter.WriteFiles(resxKeys, outputPath);
        }

        StatusMessage = $"Exported {allKeys.Count} translation key(s) to {outputPath}.";
    }

    private string ExtractBaseFileName(string filePath, FileType fileType)
    {
        return fileType == FileType.Json
            ? _jsonReader.ExtractBaseFileName(filePath)
            : _resxReader.ExtractBaseFileName(filePath);
    }

    public event EventHandler<TranslationKey>? OnEditTranslationRequested;
}

