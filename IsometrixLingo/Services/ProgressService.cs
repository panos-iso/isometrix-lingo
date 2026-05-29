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
                    Type = tk.Source.Type,
                    DirectoryPath = tk.Source.DirectoryPath
                },
                LanguageValues = new Dictionary<string, string>(tk.LanguageValues),
                SuggestedValues = tk.SuggestedValues.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new SerializableSuggestion
                    {
                        Value = kvp.Value.Value,
                        Username = kvp.Value.Username,
                        Timestamp = kvp.Value.Timestamp
                    }
                ),
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
            FileMappingStepStatus = state.FileMappingStepStatus,
            ModeSelectionStepStatus = state.ModeSelectionStepStatus,
            EditStepStatus = state.EditStepStatus,
            ExportStepStatus = state.ExportStepStatus,
            DeployStepStatus = state.DeployStepStatus,
            CurrentMode = state.CurrentMode,
            
            // Deployment-related properties
            RootDirectoryPath = state.RootDirectoryPath,
            DeploymentRootPath = state.DeploymentRootPath,
            SuggestedDeploymentRoot = state.SuggestedDeploymentRoot,
            LastExportFolder = state.LastExportFolder,
            LastExportFileName = state.LastExportFileName,
            DeploymentPreviewItems = state.DeploymentPreviewItems.Select(dpi => new SerializableDeploymentPreviewItem
            {
                SourcePath = dpi.SourcePath,
                TargetPath = dpi.TargetPath,
                IsValid = dpi.IsValid,
                ErrorMessage = dpi.ErrorMessage
            }).ToList(),
            DeploymentValidationSuccess = state.DeploymentValidationSuccess,
            DeploymentValidationMessage = state.DeploymentValidationMessage,
            ShowDeploymentSuccess = state.ShowDeploymentSuccess,
            DeploymentSuccessMessage = state.DeploymentSuccessMessage,
            DeploymentHistory = state.DeploymentHistory.Select(dh => new SerializableDeploymentHistoryEntry
            {
                Timestamp = dh.Timestamp,
                FileCount = dh.FileCount,
                Success = dh.Success,
                DeploymentRoot = dh.DeploymentRoot
            }).ToList()
        };

        var json = JsonSerializer.Serialize(serializableState, AppJsonSerializerContext.Default.SerializableSessionState);
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
            var serializableState = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.SerializableSessionState);

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
                        Source = new SourceFile(stk.Source.Name, stk.Source.Type, stk.Source.DirectoryPath),
                        LanguageValues = new Dictionary<string, string>(stk.LanguageValues),
                        SuggestedValues = stk.SuggestedValues.ToDictionary(
                            kvp => kvp.Key,
                            kvp => new Suggestion
                            {
                                Value = kvp.Value.Value,
                                Username = kvp.Value.Username,
                                Timestamp = kvp.Value.Timestamp
                            }
                        ),
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
                FileMappingStepStatus = serializableState.FileMappingStepStatus,
                ModeSelectionStepStatus = serializableState.ModeSelectionStepStatus,
                EditStepStatus = serializableState.EditStepStatus,
                ExportStepStatus = serializableState.ExportStepStatus,
                DeployStepStatus = serializableState.DeployStepStatus,
                CurrentMode = serializableState.CurrentMode,
                
                // Deployment-related properties
                RootDirectoryPath = serializableState.RootDirectoryPath,
                DeploymentRootPath = serializableState.DeploymentRootPath,
                SuggestedDeploymentRoot = serializableState.SuggestedDeploymentRoot,
                LastExportFolder = serializableState.LastExportFolder,
                LastExportFileName = serializableState.LastExportFileName,
                DeploymentPreviewItems = serializableState.DeploymentPreviewItems.Select(sdpi => new DeploymentPreviewItem
                {
                    SourcePath = sdpi.SourcePath,
                    TargetPath = sdpi.TargetPath,
                    IsValid = sdpi.IsValid,
                    ErrorMessage = sdpi.ErrorMessage
                }).ToList(),
                DeploymentValidationSuccess = serializableState.DeploymentValidationSuccess,
                DeploymentValidationMessage = serializableState.DeploymentValidationMessage,
                ShowDeploymentSuccess = serializableState.ShowDeploymentSuccess,
                DeploymentSuccessMessage = serializableState.DeploymentSuccessMessage,
                DeploymentHistory = serializableState.DeploymentHistory.Select(sdh => new DeploymentHistoryEntry
                {
                    Timestamp = sdh.Timestamp,
                    FileCount = sdh.FileCount,
                    Success = sdh.Success,
                    DeploymentRoot = sdh.DeploymentRoot
                }).ToList()
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
