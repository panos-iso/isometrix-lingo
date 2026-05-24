using CommunityToolkit.Mvvm.ComponentModel;

namespace TranslationManagementTool.Models;

public partial class FileFilterItem : ObservableObject
{
    [ObservableProperty]
    private SourceFile _source = null!;

    [ObservableProperty]
    private bool _isSelected = true;

    public string DisplayName => $"{Source.Name} ({(Source.Type == FileType.Json ? "JSON" : "RESX")})";
}
