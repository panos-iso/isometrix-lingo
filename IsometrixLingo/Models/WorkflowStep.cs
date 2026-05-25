namespace IsometrixLingo.Models;

public enum WorkflowStep
{
    Import = 1,
    Edit = 2,
    Export = 3
}

public enum StepStatus
{
    NotStarted,
    InProgress,
    Completed
}
