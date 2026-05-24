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
}
