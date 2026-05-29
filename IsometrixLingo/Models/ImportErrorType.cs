namespace IsometrixLingo.Models;

/// <summary>
/// Represents the type of error that occurred during file import.
/// </summary>
public enum ImportErrorType
{
    /// <summary>
    /// A file with the same name has already been imported.
    /// </summary>
    DuplicateFile,
    
    /// <summary>
    /// The file name doesn't match the expected naming convention.
    /// </summary>
    InvalidNamingConvention,
    
    /// <summary>
    /// The JSON file could not be parsed (malformed JSON or wrong structure).
    /// </summary>
    JsonParseError,
    
    /// <summary>
    /// The RESX file could not be parsed (malformed XML or wrong structure).
    /// </summary>
    ResxParseError,
    
    /// <summary>
    /// The language code extracted from the file name is not supported.
    /// </summary>
    UnsupportedLanguage,
    
    /// <summary>
    /// A file was not found at the expected location.
    /// </summary>
    FileNotFound,
    
    /// <summary>
    /// A file path is invalid or points outside the allowed directory.
    /// </summary>
    InvalidPath,
    
    /// <summary>
    /// An error occurred while reading a file.
    /// </summary>
    ReadError,
    
    /// <summary>
    /// An error occurred while writing a file.
    /// </summary>
    WriteError,
    
    /// <summary>
    /// An unspecified error occurred during import.
    /// </summary>
    Other
}
