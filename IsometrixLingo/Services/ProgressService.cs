using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        // Convert to serializable format
        var serializableState = new SerializableSessionState
        {
            TranslationKeys = state.TranslationKeys.Select(tk => new SerializableTranslationKey
            {
                Key = tk.Key,
                Source = new SerializableSourceFile
                {
                    Name = tk.Source.Name,
                    Type = tk.Source.Type
                },
                LanguageValues = new Dictionary<string, string>(tk.LanguageValues),
                IsModified = tk.IsModified,
                OriginalValues = new Dictionary<string, string>(tk.OriginalValues),
                ModifiedLanguages = tk.ModifiedLanguages.ToList(),
                ShowOriginalForThisRow = tk.ShowOriginalForThisRow
            }).ToList(),
            ImportedFileNames = state.ImportedFileNames,
            ResxTemplates = state.ResxTemplates,
            JsonTemplates = state.JsonTemplates,
            CurrentStep = state.CurrentStep,
            ImportStepStatus = state.ImportStepStatus,
            EditStepStatus = state.EditStepStatus,
            ExportStepStatus = state.ExportStepStatus
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false
        };

        var json = JsonSerializer.Serialize(serializableState, options);
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
            var serializableState = JsonSerializer.Deserialize<SerializableSessionState>(json);

            if (serializableState == null)
            {
                return null;
            }

            // Convert back to SessionState with ObservableObject instances
            var sessionState = new SessionState
            {
                TranslationKeys = serializableState.TranslationKeys.Select(stk =>
                {
                    var key = new TranslationKey
                    {
                        Key = stk.Key,
                        Source = new SourceFile(stk.Source.Name, stk.Source.Type),
                        LanguageValues = new Dictionary<string, string>(stk.LanguageValues),
                        IsModified = stk.IsModified,
                        OriginalValues = new Dictionary<string, string>(stk.OriginalValues),
                        ModifiedLanguages = new HashSet<string>(stk.ModifiedLanguages),
                        ShowOriginalForThisRow = stk.ShowOriginalForThisRow
                    };
                    key.UpdateMissingTranslationsStatus();
                    return key;
                }).ToList(),
                ImportedFileNames = serializableState.ImportedFileNames,
                ResxTemplates = serializableState.ResxTemplates,
                JsonTemplates = serializableState.JsonTemplates,
                CurrentStep = serializableState.CurrentStep,
                ImportStepStatus = serializableState.ImportStepStatus,
                EditStepStatus = serializableState.EditStepStatus,
                ExportStepStatus = serializableState.ExportStepStatus
            };

            return sessionState;
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
