using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.Models;

public partial class FileFilterItem : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(TruncatedDirectoryPath))]
    private SourceFile _source = null!;

    [ObservableProperty]
    private bool _isSelected = true;

    public string DisplayName => $"{Source.Name} ({(Source.Type == FileType.Json ? "JSON" : "RESX")})";

    public string? TruncatedDirectoryPath
    {
        get
        {
            if (string.IsNullOrEmpty(Source.DirectoryPath))
                return null;

            var directoryPath = Source.DirectoryPath;
            const int maxLength = 60;
            
            if (directoryPath.Length <= maxLength)
                return directoryPath;

            // Truncate in the middle
            var parts = directoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 2)
                return directoryPath;

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

    partial void OnIsSelectedChanged(bool value)
    {
        // Notify when selection changes so parent can react
        SelectionChanged?.Invoke(this, value);
    }

    public event EventHandler<bool>? SelectionChanged;
}
