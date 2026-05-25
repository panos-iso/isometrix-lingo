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

    [ObservableProperty]
    private Dictionary<string, string> _originalValues = new();

    [ObservableProperty]
    private HashSet<string> _modifiedLanguages = new();

    [ObservableProperty]
    private bool _showOriginalForThisRow;

    /// <summary>
    /// Check if a specific language value has been modified
    /// </summary>
    public bool IsLanguageModified(string language)
    {
        return ModifiedLanguages.Contains(language);
    }
}
