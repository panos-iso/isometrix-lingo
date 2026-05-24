using System.Collections.Generic;

namespace TranslationManagementTool.Helpers;

public static class LanguageHelper
{
    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        { "en", "English" },
        { "es", "Spanish" },
        { "fr", "French" },
        { "de", "German" },
        { "it", "Italian" },
        { "pt", "Portuguese" },
        { "ru", "Russian" },
        { "zh", "Chinese" },
        { "ja", "Japanese" },
        { "ko", "Korean" },
        { "ar", "Arabic" },
        { "hi", "Hindi" },
        { "nl", "Dutch" },
        { "pl", "Polish" },
        { "sv", "Swedish" },
        { "da", "Danish" },
        { "no", "Norwegian" },
        { "fi", "Finnish" },
        { "tr", "Turkish" },
        { "el", "Greek" }
    };

    public static string GetLanguageName(string languageCode)
    {
        return LanguageNames.TryGetValue(languageCode.ToLowerInvariant(), out var name) 
            ? name 
            : languageCode.ToUpperInvariant();
    }
}
