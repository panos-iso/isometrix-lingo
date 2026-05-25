using System.Collections.Generic;

namespace IsometrixLingo.Models;

public class SessionState
{
    public List<TranslationKey> TranslationKeys { get; set; } = new();
    public List<string> ImportedFileNames { get; set; } = new();
    public Dictionary<string, string> ResxTemplates { get; set; } = new();
    public Dictionary<string, string> JsonTemplates { get; set; } = new();
    public WorkflowStep CurrentStep { get; set; } = WorkflowStep.Import;
    public StepStatus ImportStepStatus { get; set; } = StepStatus.InProgress;
    public StepStatus FileMappingStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus EditStepStatus { get; set; } = StepStatus.NotStarted;
    public StepStatus ExportStepStatus { get; set; } = StepStatus.NotStarted;
}
