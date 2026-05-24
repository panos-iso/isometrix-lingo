using System;
using System.IO;
using System.Text.Json;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class UserSettingsService
{
    private readonly string _settingsFilePath;

    public UserSettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "IsometrixLingo");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public UserSettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
        var directory = Path.GetDirectoryName(settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public UserSettings? Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            if (settings != null)
            {
                settings.SettingsFilePath = _settingsFilePath;
            }
            return settings;
        }
        catch
        {
            return null;
        }
    }

    public void Save(UserSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    public bool IsFirstRun()
    {
        return !File.Exists(_settingsFilePath);
    }
}
