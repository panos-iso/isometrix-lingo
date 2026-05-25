using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class ProgressService
{
    private readonly string _progressFilePath;

    public ProgressService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "IsometrixLingo");
        Directory.CreateDirectory(appFolder);
        _progressFilePath = Path.Combine(appFolder, "progress.json");
    }

    public void SaveProgress(SessionState state)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(state, options);
        File.WriteAllText(_progressFilePath, json);
    }

    public SessionState? LoadProgress()
    {
        if (!File.Exists(_progressFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_progressFilePath);
            return JsonSerializer.Deserialize<SessionState>(json);
        }
        catch
        {
            return null;
        }
    }

    public void ClearProgress()
    {
        if (File.Exists(_progressFilePath))
        {
            File.Delete(_progressFilePath);
        }
    }

    public bool HasSavedProgress()
    {
        return File.Exists(_progressFilePath);
    }
}
