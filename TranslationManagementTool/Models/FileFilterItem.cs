using CommunityToolkit.Mvvm.ComponentModel;

namespace TranslationManagementTool.Models;

public partial class FileFilterItem : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}
