using CommunityToolkit.Mvvm.ComponentModel;
using IsometrixLingo.Services;
using IsometrixLingo.Models;

namespace IsometrixLingo.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _username = string.Empty;

    private readonly UserSettingsService _settingsService;

    public WelcomeViewModel()
    {
        _settingsService = new UserSettingsService();
    }

    public void SaveUsername()
    {
        if (!string.IsNullOrWhiteSpace(Username))
        {
            var settings = new UserSettings { Username = Username.Trim() };
            _settingsService.Save(settings);
        }
    }
}
