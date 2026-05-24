using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TranslationManagementTool.Models;

public partial class TranslationKey : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _sourceFile = string.Empty;

    [ObservableProperty]
    private Dictionary<string, string> _languageValues = new();

    [ObservableProperty]
    private bool _isModified;
}
