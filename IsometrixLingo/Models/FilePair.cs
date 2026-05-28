using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.Models;

public partial class FilePair : ObservableObject
{
    [ObservableProperty]
    private string _baseName = string.Empty;

    [ObservableProperty]
    private FileType _fileType;

    [ObservableProperty]
    private string? _directoryPath;

    [ObservableProperty]
    private string? _englishFileName;

    [ObservableProperty]
    private string? _spanishFileName;

    [ObservableProperty]
    private bool _hasEnglishFile;

    [ObservableProperty]
    private bool _hasSpanishFile;

    [ObservableProperty]
    private bool _createMissingEnglish;

    [ObservableProperty]
    private bool _createMissingSpanish;

    public bool IsMissingFile => !HasEnglishFile || !HasSpanishFile;

    public string DisplayName
    {
        get
        {
            var name = $"{BaseName} ({FileType})";
            if (!string.IsNullOrEmpty(DirectoryPath))
            {
                name += $" — {DirectoryPath}";
            }
            return name;
        }
    }

    public string StatusText
    {
        get
        {
            if (HasEnglishFile && HasSpanishFile)
                return "✓ Complete Pair";
            if (!HasEnglishFile && !HasSpanishFile)
                return "⚠ No Files";
            if (!HasEnglishFile)
                return "⚠ Missing English";
            return "⚠ Missing Spanish";
        }
    }
}
