namespace IsometrixLingo.Models;

/// <summary>
/// Represents a single file mapping in the deployment preview (source → target).
/// </summary>
public class DeploymentPreviewItem
{
    /// <summary>
    /// The source file path (from the exported ZIP).
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// The target deployment path (where the file will be copied).
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this file path was successfully validated.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
