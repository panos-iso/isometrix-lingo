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

    /// <summary>
    /// Check if a specific language value has been modified
    /// </summary>
    public bool IsLanguageModified(string language)
    {
        return ModifiedLanguages.Contains(language);
    }

    /// <summary>
    /// Update the HasMissingTranslations property based on current language values.
    /// Checks if both English and Spanish have non-empty values.
    /// </summary>
    public void UpdateMissingTranslationsStatus()
    {
        // A translation is missing if either en or es is not present or is empty
        var hasEnglish = LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
        var hasSpanish = LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
        
        HasMissingTranslations = !hasEnglish || !hasSpanish;
    }
}
