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

            LanguageValues.Add(new LanguageValueItem
            {
                LanguageCode = language,
                LanguageName = LanguageHelper.GetLanguageName(language),
                Value = value,
                IsReadOnly = mode == EditMode.Suggest // In Suggest Mode, actual values are read-only
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
                _translationStore.UpdateTranslation(_originalKey, item.LanguageCode, item.Value);
            }
        }
        else
        {
            // Suggest Mode: update or add suggestions
            foreach (var item in LanguageSuggestions)
            {
                if (!string.IsNullOrWhiteSpace(item.SuggestedValue))
                {
                    _translationKey.SuggestedValues[item.LanguageCode] = new Suggestion
                    {
                        Value = item.SuggestedValue,
                        Username = Username,
                        Timestamp = DateTime.UtcNow
                    };
                }
                else if (_translationKey.SuggestedValues.ContainsKey(item.LanguageCode))
                {
                    // Remove suggestion if field was cleared
                    _translationKey.SuggestedValues.Remove(item.LanguageCode);
                }
            }
            
            // Update missing translations status after modifying suggestions
            _translationKey.UpdateMissingTranslationsStatus();
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
