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
    public List<DeploymentPreviewItem> DeploymentPreviewItems { get; set; } = new();
    public bool DeploymentValidationSuccess { get; set; } = false;
    public string DeploymentValidationMessage { get; set; } = string.Empty;
    public bool ShowDeploymentSuccess { get; set; } = false;
    public string DeploymentSuccessMessage { get; set; } = string.Empty;
}
