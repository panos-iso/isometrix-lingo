using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.Models;

public partial class TranslationKey : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private SourceFile _source = new(string.Empty, FileType.Json);

    [ObservableProperty]
    private Dictionary<string, string> _languageValues = new();

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private Dictionary<string, string> _originalValues = new();

    [ObservableProperty]
    private HashSet<string> _modifiedLanguages = new();

    [ObservableProperty]
    private bool _showOriginalForThisRow;

    [ObservableProperty]
    private bool _hasMissingTranslations;

    [ObservableProperty]
    private Dictionary<string, Suggestion> _suggestedValues = new();

    [ObservableProperty]
    private Confirmation? _confirmedBy;
    
    /// <summary>
    /// Called when ConfirmedBy changes - notify UI that display text also changed
    /// </summary>
    partial void OnConfirmedByChanged(Confirmation? value)
    {
        OnPropertyChanged(nameof(ConfirmationDisplayText));
        OnPropertyChanged(nameof(IsConfirmed));
    }

    /// <summary>
    /// Get display text for confirmation, or empty string if not confirmed
    /// </summary>
    public string ConfirmationDisplayText => ConfirmedBy?.DisplayText ?? string.Empty;

    /// <summary>
    /// Check if a specific language value has been modified
    /// </summary>
    public bool IsLanguageModified(string language)
    {
        return ModifiedLanguages.Contains(language);
    }

    /// <summary>
    /// Check if a specific language has a suggestion
    /// </summary>
    public bool HasSuggestion(string language)
    {
        return SuggestedValues.ContainsKey(language);
    }

    /// <summary>
    /// Check if this key has any suggestions across any language
    /// </summary>
    public bool HasAnySuggestions => SuggestedValues.Count > 0;

    /// <summary>
    /// Check if this key is confirmed (has both en and es values AND confirmation audit)
    /// </summary>
    public bool IsConfirmed
    {
        get
        {
            if (ConfirmedBy == null)
                return false;

            var hasEnglish = LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
            var hasSpanish = LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);

            return hasEnglish && hasSpanish;
        }
    }

    /// <summary>
    /// Update the HasMissingTranslations property based on current language values and suggestions.
    /// A translation is missing if a language has neither a value NOR a suggestion.
    /// </summary>
    public void UpdateMissingTranslationsStatus()
    {
        // A translation is OK if it has either a value OR a suggestion
        var hasEnglish = (LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue)) 
                        || SuggestedValues.ContainsKey("en");
        var hasSpanish = (LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue))
                        || SuggestedValues.ContainsKey("es");
        
        HasMissingTranslations = !hasEnglish || !hasSpanish;
    }

    /// <summary>
    /// Accept a suggestion for a specific language, applying it as the actual value.
    /// Returns the accepted suggestion value, or null if no suggestion exists.
    /// </summary>
    public string? AcceptSuggestionForLanguage(string language)
    {
        if (!SuggestedValues.TryGetValue(language, out var suggestion))
            return null;

        // Apply the suggestion to the actual value
        LanguageValues[language] = suggestion.Value;
        
        // Remove the suggestion
        SuggestedValues.Remove(language);
        
        // Mark as modified
        ModifiedLanguages.Add(language);
        IsModified = true;
        
        // Update missing translations status
        UpdateMissingTranslationsStatus();
        
        // Trigger property change notifications to refresh UI bindings
        OnPropertyChanged(nameof(SuggestedValues));
        OnPropertyChanged(nameof(HasAnySuggestions));
        OnPropertyChanged(nameof(LanguageValues));
        OnPropertyChanged(nameof(ModifiedLanguages));
        
        return suggestion.Value;
    }

    /// <summary>
    /// Reject a suggestion for a specific language, removing it without applying.
    /// Returns true if a suggestion was rejected, false if no suggestion exists.
    /// </summary>
    public bool RejectSuggestionForLanguage(string language)
    {
        if (!SuggestedValues.ContainsKey(language))
            return false;

        // Remove the suggestion
        SuggestedValues.Remove(language);
        
        // Update missing translations status
        UpdateMissingTranslationsStatus();
        
        // Trigger property change notifications to refresh UI bindings
        OnPropertyChanged(nameof(SuggestedValues));
        OnPropertyChanged(nameof(HasAnySuggestions));
        
        return true;
    }

    /// <summary>
    /// Add or update a suggestion for a specific language.
    /// If the suggestion value is null or whitespace, removes any existing suggestion.
    /// </summary>
    public void SetSuggestionForLanguage(string language, string? suggestionValue, string username)
    {
        if (!string.IsNullOrWhiteSpace(suggestionValue))
        {
            SuggestedValues[language] = new Suggestion
            {
                Value = suggestionValue,
                Username = username,
                Timestamp = DateTime.UtcNow
            };
        }
        else if (SuggestedValues.ContainsKey(language))
        {
            // Remove suggestion if value is empty/whitespace
            SuggestedValues.Remove(language);
        }
        
        // Update missing translations status
        UpdateMissingTranslationsStatus();
        
        // Trigger property change notifications to refresh UI bindings
        OnPropertyChanged(nameof(SuggestedValues));
        OnPropertyChanged(nameof(HasAnySuggestions));
    }
}
