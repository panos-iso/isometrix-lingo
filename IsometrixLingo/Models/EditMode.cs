namespace IsometrixLingo.Models;

/// <summary>
/// Workflow mode for the translation workflow
/// </summary>
public enum EditMode
{
    /// <summary>
    /// Edit mode - directly modify translation values
    /// </summary>
    Edit,

    /// <summary>
    /// Suggest mode - create suggestions for translation values
    /// </summary>
    Suggest,

    /// <summary>
    /// Deployment mode - skip editing and deploy translations back to repository
    /// </summary>
    Deployment
}
