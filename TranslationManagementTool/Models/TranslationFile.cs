using System.Collections.Generic;

namespace TranslationManagementTool.Models;

public class TranslationFile
{
    public string FilePath { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string Language { get; set; } = string.Empty;
    public List<TranslationKey> Keys { get; set; } = new();
}
