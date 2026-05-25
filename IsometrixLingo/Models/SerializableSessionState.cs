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
    public StepStatus EditStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus ExportStepStatus { get; set; } = StepStatus.NotStarted;
}

/// <summary>
/// Serializable version of TranslationKey without ObservableObject
/// </summary>
public class SerializableTranslationKey
{
    public string Key { get; set; } = string.Empty;
    public SerializableSourceFile Source { get; set; } = new();
    public Dictionary<string, string> LanguageValues { get; set; } = new();
    public bool IsModified { get; set; }
    public Dictionary<string, string> OriginalValues { get; set; } = new();
    public List<string> ModifiedLanguages { get; set; } = new();
}

/// <summary>
/// Serializable version of SourceFile
/// </summary>
public class SerializableSourceFile
{
    public string Name { get; set; } = string.Empty;
    public FileType Type { get; set; }
}
