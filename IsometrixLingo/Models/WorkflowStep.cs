namespace IsometrixLingo.Models;

public enum WorkflowStep
{
    Import = 1,
    FileMapping = 2,
    ModeSelection = 3,
    Edit = 4,
    Export = 5
}

public enum StepStatus
{
    NotStarted,
    InProgress,
    Completed
}
