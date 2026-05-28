using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.Models;

/// <summary>
/// Represents the result of scanning a directory for translation files
/// </summary>
public partial class DirectoryScanResult : ObservableObject
{
    [ObservableProperty]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    private string _directoryName = string.Empty;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private bool _isSelected = true; // Default to selected

    [ObservableProperty]
    private List<string> _translationFiles = new();

    public string DisplayText => $"{DirectoryName} ({FileCount} file{(FileCount == 1 ? "" : "s")})";
}
