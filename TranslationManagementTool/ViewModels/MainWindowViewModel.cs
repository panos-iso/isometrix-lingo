using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslationManagementTool.Services;

namespace TranslationManagementTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TranslationStore _translationStore;
    private readonly JsonTranslationFileReader _jsonReader;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click Import to load translation files.";

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

        StatusMessage = $"Imported {files.Count} file(s) with {_translationStore.FilteredKeys.Count} translation keys.";
        LanguagesChanged?.Invoke(this, EventArgs.Empty);
    }
}
