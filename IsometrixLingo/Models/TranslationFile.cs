using System.Collections.Generic;

namespace IsometrixLingo.Models;

public class TranslationFile
{
    public string FilePath { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string Language { get; set; } = string.Empty;
    public List<TranslationKey> Keys { get; set; } = new();
    
    /// <summary>
    /// Relative directory path from bulk import root (null for individual file imports)
    /// </summary>
    public string? RelativeDirectoryPath { get; set; }
}
