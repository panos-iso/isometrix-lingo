using System;
using System.Collections.Generic;

namespace IsometrixLingo.Models;

/// <summary>
/// Serializable version of session state without ObservableObject dependencies
/// </summary>
public class SerializableSessionState
{
    public List<SerializableTranslationKey> TranslationKeys { get; set; } = new();
    public List<string> ImportedFileNames { get; set; } = new();
    public Dictionary<string, string> ResxTemplates { get; set; } = new();
    public Dictionary<string, string> JsonTemplates { get; set; } = new();
    public WorkflowStep CurrentStep { get; set; } = WorkflowStep.Import;
    public StepStatus ImportStepStatus { get; set; } = StepStatus.InProgress;
    public StepStatus FileMappingStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus ModeSelectionStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus EditStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus ExportStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus DeployStepStatus { get; set; } = StepStatus.NotStarted;
    public EditMode CurrentMode { get; set; } = EditMode.Edit;
    
    // Deployment-related properties
    public string RootDirectoryPath { get; set; } = string.Empty;
    public string DeploymentRootPath { get; set; } = string.Empty;
    public string SuggestedDeploymentRoot { get; set; } = string.Empty;
    public string LastExportFolder { get; set; } = string.Empty;
    public string LastExportFileName { get; set; } = string.Empty;
    public List<SerializableDeploymentPreviewItem> DeploymentPreviewItems { get; set; } = new();
    public bool DeploymentValidationSuccess { get; set; } = false;
    public string DeploymentValidationMessage { get; set; } = string.Empty;
    public bool ShowDeploymentSuccess { get; set; } = false;
    public string DeploymentSuccessMessage { get; set; } = string.Empty;
}

/// <summary>
/// Serializable version of TranslationKey without ObservableObject
/// </summary>
public class SerializableTranslationKey
{
    public string Key { get; set; } = string.Empty;
    public SerializableSourceFile Source { get; set; } = new();
    public Dictionary<string, string> LanguageValues { get; set; } = new();
    public Dictionary<string, SerializableSuggestion> SuggestedValues { get; set; } = new();
    public SerializableConfirmation? ConfirmedBy { get; set; }
    public bool IsModified { get; set; }
    public Dictionary<string, string> OriginalValues { get; set; } = new();
    public List<string> ModifiedLanguages { get; set; } = new();
    public bool ShowOriginalForThisRow { get; set; }
}

/// <summary>
/// Serializable version of SourceFile
/// </summary>
public class SerializableSourceFile
{
    public string Name { get; set; } = string.Empty;
    public FileType Type { get; set; }
    public string? DirectoryPath { get; set; }
}

/// <summary>
/// Serializable version of Suggestion
/// </summary>
public class SerializableSuggestion
{
    public string Value { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Serializable version of Confirmation
/// </summary>
public class SerializableConfirmation
{
    public string Username { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Serializable version of DeploymentPreviewItem
/// </summary>
public class SerializableDeploymentPreviewItem
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
