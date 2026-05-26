namespace IsometrixLingo.Models;

/// <summary>
/// Represents an error that occurred during file import.
/// </summary>
public class ImportError
{
    /// <summary>
    /// The type of error that occurred.
    /// </summary>
    public ImportErrorType ErrorType { get; set; }
    
    /// <summary>
    /// The name of the file that caused the error.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// A detailed error message describing what went wrong.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Guidance text to help the user fix the error.
    /// </summary>
    public string Guidance { get; set; } = string.Empty;
}
