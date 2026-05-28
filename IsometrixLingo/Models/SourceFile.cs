namespace IsometrixLingo.Models;

/// <summary>
/// Represents a source translation file with its name, type, and optional directory path
/// DirectoryPath is the full relative path from the bulk import root directory
/// (null for individual file imports)
/// </summary>
public record SourceFile(string Name, FileType Type, string? DirectoryPath = null);
