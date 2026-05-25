using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class TranslationStore
{
    private readonly List<TranslationKey> _allKeys = new();
    private readonly ObservableCollection<TranslationKey> _filteredKeys = new();
    private readonly List<SourceFile> _sourceFiles = new();
    private readonly Dictionary<string, XDocument> _resxTemplates = new();
    private readonly Dictionary<string, string> _jsonTemplates = new();
    private static readonly List<string> _supportedLanguages = new() { "en", "es" };
    private List<SourceFile>? _currentFileFilter = null;
    private string _currentSearchTerm = string.Empty;
    private bool _hasUnsavedChanges = false;

    public ObservableCollection<TranslationKey> FilteredKeys => _filteredKeys;
    public IReadOnlyCollection<SourceFile> SourceFiles => _sourceFiles;
    public IReadOnlyCollection<string> Languages => _supportedLanguages;
    public bool HasUnsavedChanges => _hasUnsavedChanges;

    public event EventHandler? UnsavedChangesChanged;

    public void AddTranslations(List<TranslationKey> keys)
    {
        foreach (var key in keys)
        {
            _allKeys.Add(key);

            // Add source file if not already tracked
            if (!_sourceFiles.Any(sf => sf.Name == key.Source.Name && sf.Type == key.Source.Type))
            {
                _sourceFiles.Add(key.Source);
            }

            // Filter out unsupported languages from the key
            var unsupportedLanguages = key.LanguageValues.Keys
                .Where(lang => !_supportedLanguages.Contains(lang))
                .ToList();

            foreach (var lang in unsupportedLanguages)
            {
                key.LanguageValues.Remove(lang);
            }
        }
        RefreshFilteredKeys();
    }

    public void Clear()
    {
        _allKeys.Clear();
        _filteredKeys.Clear();
        _sourceFiles.Clear();
        _resxTemplates.Clear();
        _jsonTemplates.Clear();
    }

    public void SetResxTemplate(string sourceFileName, XDocument template)
    {
        _resxTemplates[sourceFileName] = template;
    }

    public XDocument? GetResxTemplate(string sourceFileName)
    {
        return _resxTemplates.TryGetValue(sourceFileName, out var template) ? template : null;
    }

    public void SetJsonTemplate(string sourceFileName, string template)
    {
        _jsonTemplates[sourceFileName] = template;
    }

    public string? GetJsonTemplate(string sourceFileName)
    {
        return _jsonTemplates.TryGetValue(sourceFileName, out var template) ? template : null;
    }

    public void FilterBySourceFiles(List<SourceFile>? sourceFiles)
    {
        _currentFileFilter = sourceFiles;
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
            keysToShow = _allKeys.Where(k => _currentFileFilter.Any(sf =>
                sf.Name == k.Source.Name && sf.Type == k.Source.Type));
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
            // Store original value if this is the first edit for this language
            if (!translationKey.OriginalValues.ContainsKey(language))
            {
                translationKey.OriginalValues[language] = translationKey.LanguageValues.TryGetValue(language, out var originalValue) 
                    ? originalValue 
                    : string.Empty;
            }

            // Update the value
            translationKey.LanguageValues[language] = newValue;
            
            // Check if value actually changed from original
            var original = translationKey.OriginalValues[language];
            if (newValue != original)
            {
                translationKey.ModifiedLanguages.Add(language);
                translationKey.IsModified = true;
            }
            else
            {
                // Value was reverted to original - remove from modified set
                translationKey.ModifiedLanguages.Remove(language);
                translationKey.IsModified = translationKey.ModifiedLanguages.Count > 0;
            }

            SetUnsavedChanges(true);

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

    public List<TranslationKey> GetAllKeys()
    {
        return _allKeys.ToList();
    }

    public void AddKey(TranslationKey key)
    {
        _allKeys.Add(key);

        // Add source file if not already tracked
        if (!_sourceFiles.Any(sf => sf.Name == key.Source.Name && sf.Type == key.Source.Type))
        {
            _sourceFiles.Add(key.Source);
        }

        SetUnsavedChanges(true);
        RefreshFilteredKeys();
    }

    public void MarkAllChangesSaved()
    {
        SetUnsavedChanges(false);
    }

    public Dictionary<string, string> GetAllResxTemplates()
    {
        var templates = new Dictionary<string, string>();
        foreach (var kvp in _resxTemplates)
        {
            templates[kvp.Key] = kvp.Value.ToString();
        }
        return templates;
    }

    public Dictionary<string, string> GetAllJsonTemplates()
    {
        return new Dictionary<string, string>(_jsonTemplates);
    }

    public void RestoreResxTemplates(Dictionary<string, string> templates)
    {
        _resxTemplates.Clear();
        foreach (var kvp in templates)
        {
            _resxTemplates[kvp.Key] = XDocument.Parse(kvp.Value);
        }
    }

    public void RestoreJsonTemplates(Dictionary<string, string> templates)
    {
        _jsonTemplates.Clear();
        foreach (var kvp in templates)
        {
            _jsonTemplates[kvp.Key] = kvp.Value;
        }
    }

    private void SetUnsavedChanges(bool value)
    {
        if (_hasUnsavedChanges != value)
        {
            _hasUnsavedChanges = value;
            UnsavedChangesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RefreshFilteredKeys()
    {
        ApplyFilters();
    }
}
