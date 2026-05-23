using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.Services;

public class TranslationStore
{
    private readonly List<TranslationKey> _allKeys = new();
    private readonly ObservableCollection<TranslationKey> _filteredKeys = new();
    private readonly HashSet<string> _sourceFiles = new();
    private readonly HashSet<string> _languages = new();

    public ObservableCollection<TranslationKey> FilteredKeys => _filteredKeys;
    public IReadOnlyCollection<string> SourceFiles => _sourceFiles;
    public IReadOnlyCollection<string> Languages => _languages;

    public void AddTranslations(List<TranslationKey> keys)
    {
        foreach (var key in keys)
        {
            _allKeys.Add(key);
            _sourceFiles.Add(key.SourceFile);
            
            // Track all languages found in this key
            foreach (var language in key.LanguageValues.Keys)
            {
                _languages.Add(language);
            }
        }
        RefreshFilteredKeys();
    }

    public void Clear()
    {
        _allKeys.Clear();
        _filteredKeys.Clear();
        _sourceFiles.Clear();
        _languages.Clear();
    }

    public void FilterBySourceFiles(List<string>? fileNames)
    {
        _filteredKeys.Clear();

        if (fileNames == null || fileNames.Count == 0)
        {
            // No filters selected - show all
            foreach (var key in _allKeys)
            {
                _filteredKeys.Add(key);
            }
        }
        else
        {
            // Show only keys from selected files
            foreach (var key in _allKeys.Where(k => fileNames.Contains(k.SourceFile)))
            {
                _filteredKeys.Add(key);
            }
        }
    }

    public List<TranslationKey> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return _filteredKeys.ToList();
        }

        var term = searchTerm.ToLowerInvariant();
        return _filteredKeys.Where(k =>
            k.Key.ToLowerInvariant().Contains(term) ||
            k.LanguageValues.Any(lv => lv.Value.ToLowerInvariant().Contains(term))
        ).ToList();
    }

    public void UpdateTranslation(string key, string language, string newValue)
    {
        var translationKey = _allKeys.FirstOrDefault(k => k.Key == key);
        if (translationKey != null)
        {
            translationKey.LanguageValues[language] = newValue;
            translationKey.IsModified = true;
        }
    }

    public List<TranslationKey> GetModifiedKeys()
    {
        return _allKeys.Where(k => k.IsModified).ToList();
    }

    private void RefreshFilteredKeys()
    {
        FilterBySourceFiles(null!); // Show all by default
    }
}
