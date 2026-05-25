namespace IsometrixLingo.Models;

public enum WorkflowStep
{
    Import = 1,
    FileMapping = 2,
    Edit = 3,
    Export = 4
}

public enum StepStatus
{
    NotStarted,
    InProgress,
    Completed
}
