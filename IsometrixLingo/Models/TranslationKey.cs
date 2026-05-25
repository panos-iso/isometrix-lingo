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
}
