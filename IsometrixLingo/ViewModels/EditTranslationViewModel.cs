using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IsometrixLingo.Helpers;
using IsometrixLingo.Models;
using IsometrixLingo.Services;

namespace IsometrixLingo.ViewModels;

public partial class EditTranslationViewModel : ViewModelBase
{
    private readonly TranslationStore _translationStore;
    private readonly TranslationKey _translationKey;
    private readonly string _originalKey;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private EditMode _currentMode = EditMode.Edit;

    [ObservableProperty]
    private string _username = "User";

    [ObservableProperty]
    private ObservableCollection<LanguageValueItem> _languageValues = new();

    [ObservableProperty]
    private ObservableCollection<LanguageSuggestionItem> _languageSuggestions = new();

    // Design-time constructor
    public EditTranslationViewModel()
    {
        _translationStore = new TranslationStore();
        _translationKey = new TranslationKey();
        _originalKey = string.Empty;
    }

    public EditTranslationViewModel(TranslationKey translationKey, TranslationStore translationStore, EditMode mode, string username)
    {
        _translationStore = translationStore;
        _translationKey = translationKey;
        _originalKey = translationKey.Key;
        CurrentMode = mode;
        Username = username;
        Key = translationKey.Key;

        // Always show all supported languages
        foreach (var language in translationStore.Languages.OrderBy(l => l))
        {
            var value = translationKey.LanguageValues.TryGetValue(language, out var existingValue)
                ? existingValue
                : string.Empty;

            var hasSuggestion = translationKey.SuggestedValues.TryGetValue(language, out var suggestionObj);
            var suggestionText = hasSuggestion
                ? $"→ {suggestionObj!.Value} ({suggestionObj.Username}, {suggestionObj.Timestamp:MMM dd})"
                : string.Empty;

            LanguageValues.Add(new LanguageValueItem
            {
                LanguageCode = language,
                LanguageName = LanguageHelper.GetLanguageName(language),
                Value = value,
                IsReadOnly = mode == EditMode.Suggest, // In Suggest Mode, actual values are read-only
                HasSuggestion = hasSuggestion,
                SuggestionText = suggestionText,
                Suggestion = hasSuggestion ? suggestionObj : null
            });

            // Load existing suggestion if any
            var existingSuggestion = translationKey.SuggestedValues.TryGetValue(language, out var suggestion)
                ? suggestion.Value
                : string.Empty;

            LanguageSuggestions.Add(new LanguageSuggestionItem
            {
                LanguageCode = language,
                LanguageName = LanguageHelper.GetLanguageName(language),
                CurrentValue = value, // Add current value for side-by-side display
                SuggestedValue = existingSuggestion
            });
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (CurrentMode == EditMode.Edit)
        {
            // Edit Mode: update actual values
            foreach (var item in LanguageValues)
            {
                _translationStore.UpdateTranslation(_originalKey, item.LanguageCode, item.Value ?? string.Empty);
                
                // If there was a suggestion and user manually edited, remove the suggestion
                if (item.HasSuggestion)
                {
                    _translationKey.RejectSuggestionForLanguage(item.LanguageCode);
                }
            }
        }
        else
        {
            // Suggest Mode: update or add suggestions using TranslationKey's method
            foreach (var item in LanguageSuggestions)
            {
                _translationKey.SetSuggestionForLanguage(item.LanguageCode, item.SuggestedValue, Username);
            }
        }
    }
    
    [RelayCommand]
    private void AcceptSuggestion(LanguageValueItem item)
    {
        if (item.Suggestion == null) return;
        
        // Apply suggestion value to the text box
        item.Value = item.Suggestion.Value;
        
        // Remove the suggestion from the translation key
        var acceptedValue = _translationKey.AcceptSuggestionForLanguage(item.LanguageCode);
        
        if (acceptedValue != null)
        {
            // Update UI to hide suggestion
            item.HasSuggestion = false;
            item.SuggestionText = string.Empty;
            item.Suggestion = null;
        }
    }
    
    [RelayCommand]
    private void RejectSuggestion(LanguageValueItem item)
    {
        // Remove the suggestion from the translation key
        var wasRejected = _translationKey.RejectSuggestionForLanguage(item.LanguageCode);
        
        if (wasRejected)
        {
            // Update UI to hide suggestion
            item.HasSuggestion = false;
            item.SuggestionText = string.Empty;
            item.Suggestion = null;
        }
    }
}

public partial class LanguageValueItem : ObservableObject
{
    [ObservableProperty]
    private string _languageCode = string.Empty;

    [ObservableProperty]
    private string _languageName = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isReadOnly = false;
    
    [ObservableProperty]
    private bool _hasSuggestion = false;
    
    [ObservableProperty]
    private string _suggestionText = string.Empty;
    
    public Suggestion? Suggestion { get; set; }
}

public partial class LanguageSuggestionItem : ObservableObject
{
    [ObservableProperty]
    private string _languageCode = string.Empty;

    [ObservableProperty]
    private string _languageName = string.Empty;

    [ObservableProperty]
    private string _currentValue = string.Empty;

    [ObservableProperty]
    private string _suggestedValue = string.Empty;
}
