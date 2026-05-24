using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.Services;

public class ProgressService
{
    private readonly string _progressFilePath;

    public ProgressService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "TranslationManagementTool");
        Directory.CreateDirectory(appFolder);
        _progressFilePath = Path.Combine(appFolder, "progress.json");
    }

    public void SaveProgress(List<TranslationKey> keys)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(keys, options);
        File.WriteAllText(_progressFilePath, json);
    }

    public List<TranslationKey>? LoadProgress()
    {
        if (!File.Exists(_progressFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_progressFilePath);
            return JsonSerializer.Deserialize<List<TranslationKey>>(json);
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
