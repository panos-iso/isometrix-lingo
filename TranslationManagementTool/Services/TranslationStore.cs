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
    private List<string>? _currentFileFilter = null;
    private string _currentSearchTerm = string.Empty;

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
        _currentFileFilter = fileNames;
        ApplyFilters();
    }

    public void FilterBySearchTerm(string searchTerm)
    {
        _currentSearchTerm = searchTerm ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        _filteredKeys.Clear();

        // Start with all keys or file-filtered keys
        IEnumerable<TranslationKey> keysToShow;
        
        if (_currentFileFilter == null || _currentFileFilter.Count == 0)
        {
            keysToShow = _allKeys;
        }
        else
        {
            keysToShow = _allKeys.Where(k => _currentFileFilter.Contains(k.SourceFile));
        }

        // Apply search filter if present
        if (!string.IsNullOrWhiteSpace(_currentSearchTerm))
        {
            var term = _currentSearchTerm.ToLowerInvariant();
            keysToShow = keysToShow.Where(k =>
                k.Key.ToLowerInvariant().Contains(term) ||
                k.LanguageValues.Any(lv => lv.Value.ToLowerInvariant().Contains(term))
            );
        }

        foreach (var key in keysToShow)
        {
            _filteredKeys.Add(key);
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
            
            // Trigger property change notification for the dictionary
            // This is a workaround: we reassign to trigger INotifyPropertyChanged
            var temp = translationKey.LanguageValues;
            translationKey.LanguageValues = new Dictionary<string, string>(temp);
        }
    }

    public void RefreshUI()
    {
        // Force UI refresh by re-applying filters
        ApplyFilters();
    }

    public List<TranslationKey> GetModifiedKeys()
    {
        return _allKeys.Where(k => k.IsModified).ToList();
    }

    private void RefreshFilteredKeys()
    {
        ApplyFilters();
    }
}
