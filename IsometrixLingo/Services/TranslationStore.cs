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
    private bool _showOnlyMissingTranslations = false;
    private bool _showOnlyWithSuggestions = false;
    private bool _showOnlyUnconfirmed = false;
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
            // Check if this key already exists (same key name and source file)
            var existingKey = _allKeys.FirstOrDefault(k => 
                k.Key == key.Key && 
                k.Source.Name == key.Source.Name && 
                k.Source.Type == key.Source.Type);

            if (existingKey != null)
            {
                // Merge language values into existing key
                var mergedValues = new Dictionary<string, string>(existingKey.LanguageValues);
                foreach (var langValue in key.LanguageValues)
                {
                    // Only add supported languages
                    if (_supportedLanguages.Contains(langValue.Key))
                    {
                        mergedValues[langValue.Key] = langValue.Value;
                    }
                }
                existingKey.LanguageValues = mergedValues;

                // Merge suggested values into existing key
                var mergedSuggestions = new Dictionary<string, Suggestion>(existingKey.SuggestedValues);
                foreach (var suggestion in key.SuggestedValues)
                {
                    // Only add supported languages
                    if (_supportedLanguages.Contains(suggestion.Key))
                    {
                        mergedSuggestions[suggestion.Key] = suggestion.Value;
                    }
                }
                existingKey.SuggestedValues = mergedSuggestions;

                // Merge confirmation if present in the new key
                if (key.ConfirmedBy != null)
                {
                    existingKey.ConfirmedBy = key.ConfirmedBy;
                }

                // Update missing translations status
                existingKey.UpdateMissingTranslationsStatus();
            }
            else
            {
                // Filter out unsupported languages from the key
                var unsupportedLanguages = key.LanguageValues.Keys
                    .Where(lang => !_supportedLanguages.Contains(lang))
                    .ToList();

                foreach (var lang in unsupportedLanguages)
                {
                    key.LanguageValues.Remove(lang);
                }

                // Add as new key
                _allKeys.Add(key);
            }

            // Add source file if not already tracked
            if (!_sourceFiles.Any(sf => sf.Name == key.Source.Name && sf.Type == key.Source.Type))
            {
                _sourceFiles.Add(key.Source);
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

    public void FilterByMissingTranslations(bool showOnlyMissing)
    {
        _showOnlyMissingTranslations = showOnlyMissing;
        ApplyFilters();
    }

    public void FilterBySuggestions(bool showOnlyWithSuggestions)
    {
        _showOnlyWithSuggestions = showOnlyWithSuggestions;
        ApplyFilters();
    }

    public void FilterByConfirmation(bool showOnlyUnconfirmed)
    {
        _showOnlyUnconfirmed = showOnlyUnconfirmed;
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
                k.LanguageValues.Any(lv => lv.Value.ToLowerInvariant().Contains(term)) ||
                k.SuggestedValues.Any(sv => sv.Value.Value.ToLowerInvariant().Contains(term))
            );
        }

        // Apply missing translations filter if enabled
        if (_showOnlyMissingTranslations)
        {
            keysToShow = keysToShow.Where(k => k.HasMissingTranslations);
        }

        // Apply suggestions filter if enabled
        if (_showOnlyWithSuggestions)
        {
            keysToShow = keysToShow.Where(k => k.HasAnySuggestions);
        }

        // Apply unconfirmed filter if enabled
        if (_showOnlyUnconfirmed)
        {
            keysToShow = keysToShow.Where(k => !k.IsConfirmed);
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
                var original = translationKey.LanguageValues.TryGetValue(language, out var originalValue)
                    ? originalValue
                    : string.Empty;

                // Create new dictionary to trigger property change
                var newOriginals = new Dictionary<string, string>(translationKey.OriginalValues)
                {
                    [language] = original
                };
                translationKey.OriginalValues = newOriginals;
            }

            // Update the value - create new dictionary to trigger property change
            var newValues = new Dictionary<string, string>(translationKey.LanguageValues)
            {
                [language] = newValue
            };
            translationKey.LanguageValues = newValues;

            // Check if value actually changed from original
            var originalStoredValue = translationKey.OriginalValues[language];
            var newModified = new HashSet<string>(translationKey.ModifiedLanguages);

            if (newValue != originalStoredValue)
            {
                newModified.Add(language);
                translationKey.IsModified = true;
            }
            else
            {
                // Value was reverted to original - remove from modified set
                newModified.Remove(language);
                translationKey.IsModified = newModified.Count > 0;
            }

            translationKey.ModifiedLanguages = newModified;
            translationKey.UpdateMissingTranslationsStatus();

            SetUnsavedChanges(true);
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
        key.UpdateMissingTranslationsStatus();

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
