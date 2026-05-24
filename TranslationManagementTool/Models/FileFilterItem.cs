using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TranslationManagementTool.Models;

public partial class FileFilterItem : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private SourceFile _source = null!;

    [ObservableProperty]
    private bool _isSelected = true;

    public string DisplayName => $"{Source.Name} ({(Source.Type == FileType.Json ? "JSON" : "RESX")})";

    partial void OnIsSelectedChanged(bool value)
    {
        // Notify when selection changes so parent can react
        SelectionChanged?.Invoke(this, value);
    }

    public event EventHandler<bool>? SelectionChanged;
}
