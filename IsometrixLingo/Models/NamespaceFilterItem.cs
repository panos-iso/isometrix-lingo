using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.Models;

public partial class NamespaceFilterItem : ObservableObject
{
    [ObservableProperty]
    private string _namespace = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private int _fileCount;

    public string DisplayName => Namespace == string.Empty ? "All Namespaces" : Namespace;

    partial void OnIsSelectedChanged(bool value)
    {
        // Notify when selection changes so parent can react
        SelectionChanged?.Invoke(this, value);
    }

    public event EventHandler<bool>? SelectionChanged;
}
