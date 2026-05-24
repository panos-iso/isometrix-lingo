using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslationManagementTool.Helpers;
using TranslationManagementTool.Models;
using TranslationManagementTool.Services;

namespace TranslationManagementTool.ViewModels;

public partial class EditTranslationViewModel : ViewModelBase
{
    private readonly TranslationStore _translationStore;
    private readonly string _originalKey;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LanguageValueItem> _languageValues = new();

    // Design-time constructor
    public EditTranslationViewModel()
    {
        _translationStore = new TranslationStore();
        _originalKey = string.Empty;
    }

    public EditTranslationViewModel(TranslationKey translationKey, TranslationStore translationStore)
    {
        _translationStore = translationStore;
        _originalKey = translationKey.Key;
        Key = translationKey.Key;

        // Always show all supported languages (en and es)
        foreach (var language in translationStore.Languages.OrderBy(l => l))
        {
            var value = translationKey.LanguageValues.TryGetValue(language, out var existingValue)
                ? existingValue
                : string.Empty;

            LanguageValues.Add(new LanguageValueItem
            {
                LanguageCode = language,
                LanguageName = LanguageHelper.GetLanguageName(language),
                Value = value
            });
        }
    }

    [RelayCommand]
    private void Save()
    {
        foreach (var item in LanguageValues)
        {
            _translationStore.UpdateTranslation(_originalKey, item.LanguageCode, item.Value);
        }
    }
}

public partial class LanguageValueItem : ObservableObject
{
    [ObservableProperty]
    private string _languageCode = string.Empty;

    [ObservableProperty]
    private string _languageName = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}
