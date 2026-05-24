using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.ViewModels;

public partial class AddKeyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _keyName = string.Empty;

    [ObservableProperty]
    private SourceFile? _selectedSourceFile;

    public ObservableCollection<SourceFile> AvailableSourceFiles { get; } = new();
    public ObservableCollection<string> AvailableLanguages { get; } = new();
    public ObservableCollection<LanguageValueItem> LanguageValues { get; } = new();

    public AddKeyViewModel(IEnumerable<SourceFile> sourceFiles, IEnumerable<string> languages, SourceFile? defaultSourceFile = null)
    {
        foreach (var file in sourceFiles)
        {
            AvailableSourceFiles.Add(file);
        }

        // Set default source file if provided and it exists in the list
        if (defaultSourceFile != null && AvailableSourceFiles.Contains(defaultSourceFile))
        {
            SelectedSourceFile = defaultSourceFile;
        }
        // Otherwise, leave it null (no default selection)

        foreach (var lang in languages)
        {
            AvailableLanguages.Add(lang);
            LanguageValues.Add(new LanguageValueItem { LanguageCode = lang, LanguageName = lang, Value = string.Empty });
        }
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(KeyName) && SelectedSourceFile != null;
    }

    public TranslationKey CreateTranslationKey()
    {
        var translationKey = new TranslationKey
        {
            Key = KeyName.Trim(),
            Source = SelectedSourceFile!,
            LanguageValues = new Dictionary<string, string>()
        };

        foreach (var langValue in LanguageValues.Where(lv => !string.IsNullOrWhiteSpace(lv.Value)))
        {
            translationKey.LanguageValues[langValue.LanguageCode] = langValue.Value;
        }

        return translationKey;
    }
}
