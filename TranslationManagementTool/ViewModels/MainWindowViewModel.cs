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
    private readonly JsonTranslationFileWriter _jsonWriter;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click Import to load translation files.";

    [ObservableProperty]
    private ObservableCollection<FileFilterItem> _fileFilters = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _hasModifiedKeys;

    public event EventHandler? LanguagesChanged;

    public MainWindowViewModel()
    {
        _translationStore = new TranslationStore();
        _jsonReader = new JsonTranslationFileReader();
        _jsonWriter = new JsonTranslationFileWriter();
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
                    Patterns = new[] { "*.json" }
                }
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        var loadedFiles = new List<string>();
        foreach (var file in files)
        {
            try
            {
                var filePath = file.Path.LocalPath;
                var translationFile = _jsonReader.ReadFile(filePath);
                loadedFiles.Add(filePath);
            }
            catch
            {
                // Skip invalid files
            }
        }

        // Group files by base name and consolidate
        var groupedFiles = files
            .Select(f => _jsonReader.ReadFile(f.Path.LocalPath))
            .GroupBy(tf => _jsonReader.ExtractBaseFileName(tf.FilePath))
            .ToList();

        foreach (var group in groupedFiles)
        {
            var consolidated = _jsonReader.ConsolidateKeys(group.ToList());
            _translationStore.AddTranslations(consolidated);
        }

        UpdateFileFilters();
        StatusMessage = $"Imported {files.Count} file(s) with {_translationStore.FilteredKeys.Count} translation keys.";
        LanguagesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ToggleFileFilter(FileFilterItem filter)
    {
        filter.IsSelected = !filter.IsSelected;
        ApplyFilters();
    }

    private void UpdateFileFilters()
    {
        FileFilters.Clear();
        foreach (var sourceFile in _translationStore.SourceFiles.OrderBy(f => f))
        {
            var filterItem = new FileFilterItem { FileName = sourceFile, IsSelected = true };
            FileFilters.Add(filterItem);
        }
    }

    private void ApplyFilters()
    {
        var selectedFiles = FileFilters
            .Where(f => f.IsSelected)
            .Select(f => f.FileName)
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
        var modifiedKeys = _translationStore.GetModifiedKeys();
        
        if (modifiedKeys.Count == 0)
        {
            StatusMessage = "No modified translations to export.";
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
        _jsonWriter.WriteFiles(modifiedKeys, outputPath);

        StatusMessage = $"Exported {modifiedKeys.Count} modified key(s) to {outputPath}. Modifications cleared.";
        
        // Clear modification flags after successful export
        foreach (var key in modifiedKeys)
        {
            key.IsModified = false;
        }
        HasModifiedKeys = false;
    }

    public event EventHandler<TranslationKey>? OnEditTranslationRequested;
}
