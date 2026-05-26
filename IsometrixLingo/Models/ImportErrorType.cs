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
    /// An unspecified error occurred during import.
    /// </summary>
    Other
}
