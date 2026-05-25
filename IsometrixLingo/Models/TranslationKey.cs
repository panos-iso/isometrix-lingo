using System.Collections.Generic;
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

    /// <summary>
    /// Original values before any edits (for showing changes)
    /// </summary>
    public Dictionary<string, string> OriginalValues { get; set; } = new();

    /// <summary>
    /// Track which specific languages have been modified
    /// </summary>
    public HashSet<string> ModifiedLanguages { get; set; } = new();

    /// <summary>
    /// Check if a specific language value has been modified
    /// </summary>
    public bool IsLanguageModified(string language)
    {
        return ModifiedLanguages.Contains(language);
    }
}
