using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    [ObservableProperty]
    private string _statusMessage = "Ready. Click Import to load translation files.";

    [ObservableProperty]
    private ObservableCollection<FileFilterItem> _fileFilters = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    public event EventHandler? LanguagesChanged;

    public MainWindowViewModel()
    {
        _translationStore = new TranslationStore();
        _jsonReader = new JsonTranslationFileReader();
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

    public event EventHandler<TranslationKey>? OnEditTranslationRequested;
}
