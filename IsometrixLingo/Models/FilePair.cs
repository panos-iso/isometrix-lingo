using System;
using System.Linq;
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
    private string? _minimalDisplayPath;

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

    public string DisplayName => $"{BaseName} ({FileType})";

    public string? TruncatedDirectoryPath
    {
        get
        {
            if (string.IsNullOrEmpty(DirectoryPath))
                return null;

            const int maxLength = 60;
            if (DirectoryPath.Length <= maxLength)
                return DirectoryPath;

            // Truncate in the middle
            var parts = DirectoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 2)
                return DirectoryPath;

            // Show first part, "...", and last 2 parts
            var firstPart = parts[0];
            var lastParts = string.Join("/", parts.Skip(parts.Length - 2));
            var truncated = $"{firstPart}/.../{ lastParts}";

            // If still too long, just use last part
            if (truncated.Length > maxLength)
            {
                truncated = $".../{parts[parts.Length - 1]}";
            }

            return truncated;
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
