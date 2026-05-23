using System.Collections.Generic;

namespace TranslationManagementTool.Models;

public class TranslationKey
{
    public string Key { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public Dictionary<string, string> LanguageValues { get; set; } = new();
    public bool IsModified { get; set; }
}
