using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class JsonTranslationFileReader
{
    private static readonly Regex LanguagePattern = new(@"[_\.]([a-z]{2})\.json$", RegexOptions.IgnoreCase);

    public TranslationFile ReadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var language = ExtractLanguage(filePath);
        var baseFileName = ExtractBaseFileName(filePath);
        var json = File.ReadAllText(filePath);

        var translationFile = new TranslationFile
        {
            FilePath = filePath,
            FileType = FileType.Json,
            Language = language,
            Keys = ParseJsonKeys(json, baseFileName, language)
        };

        return translationFile;
    }

    public string ExtractLanguage(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var match = LanguagePattern.Match(fileName);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.ToLowerInvariant();
        }

        return "unknown";
    }

    public string ExtractBaseFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var match = LanguagePattern.Match(Path.GetFileName(filePath));

        if (match.Success)
        {
            // Remove language suffix (e.g., "forms_en" → "forms")
            return fileName.Substring(0, fileName.Length - 3);
        }

        return fileName;
    }

    public List<TranslationKey> ConsolidateKeys(List<TranslationFile> files)
    {
        // Use a list to preserve the order keys are encountered (first file wins for order)
        var consolidatedKeys = new List<TranslationKey>();
        var keyIndex = new Dictionary<string, int>(); // Track which index each key is at
        var baseFileName = files.FirstOrDefault()?.FilePath;
        var baseName = baseFileName != null ? ExtractBaseFileName(baseFileName) : "unknown";
        var directoryPath = files.FirstOrDefault()?.RelativeDirectoryPath;

        // Collect all unique keys across all files IN ORDER
        // Keys from first file establish the order
        // New keys from subsequent files are APPENDED AT THE END
        foreach (var file in files)
        {
            foreach (var key in file.Keys)
            {
                if (!keyIndex.ContainsKey(key.Key))
                {
                    // New key not seen before - APPEND to end of list
                    var translationKey = new TranslationKey
                    {
                        Key = key.Key,
                        Source = new SourceFile(baseName, FileType.Json, directoryPath),
                        LanguageValues = new Dictionary<string, string>(),
                        SuggestedValues = new Dictionary<string, Suggestion>()
                    };
                    consolidatedKeys.Add(translationKey);
                    keyIndex[key.Key] = consolidatedKeys.Count - 1;
                }

                var index = keyIndex[key.Key];
                
                // Add language value
                if (key.LanguageValues.ContainsKey(file.Language))
                {
                    consolidatedKeys[index].LanguageValues[file.Language] = key.LanguageValues[file.Language];
                }
                
                // Add suggestion if present
                if (key.SuggestedValues.ContainsKey(file.Language))
                {
                    consolidatedKeys[index].SuggestedValues[file.Language] = key.SuggestedValues[file.Language];
                }
            }
        }

        // Fill in missing language values with empty strings for consistency
        var allLanguages = files.Select(f => f.Language).Distinct().ToList();
        foreach (var translationKey in consolidatedKeys)
        {
            foreach (var language in allLanguages)
            {
                if (!translationKey.LanguageValues.ContainsKey(language))
                {
                    translationKey.LanguageValues[language] = string.Empty;
                }
            }
            translationKey.UpdateMissingTranslationsStatus();
        }

        return consolidatedKeys;
    }

    private List<TranslationKey> ParseJsonKeys(string json, string baseFileName, string language)
    {
        var keys = new List<TranslationKey>();

        try
        {
            var document = JsonDocument.Parse(json);
            ParseJsonElement(document.RootElement, string.Empty, keys, baseFileName, language);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
        }

        return keys;
    }

    /// <summary>
    /// Parse a value string that may contain a suggestion
    /// Format: "actual value iso-lingo-audit:SUGGESTION:suggested_value,by:[username],at:[datetime]"
    /// Returns the actual value and parsed suggestion (if present)
    /// </summary>
    private (string actualValue, Suggestion? suggestion) ParseValue(string rawValue, string language)
    {
        const string suggestionPrefix = " iso-lingo-audit:SUGGESTION:";
        
        var actualValue = rawValue;
        Suggestion? suggestion = null;
        
        // Check for SUGGESTION
        var suggestionIndex = rawValue.IndexOf(suggestionPrefix, StringComparison.Ordinal);
        if (suggestionIndex != -1)
        {
            actualValue = rawValue.Substring(0, suggestionIndex);
            var suggestionPart = rawValue.Substring(suggestionIndex + 1); // Skip the leading space
            suggestion = Suggestion.FromFileFormat(suggestionPart);
        }
        
        return (actualValue, suggestion);
    }

    private void ParseJsonElement(JsonElement element, string prefix, List<TranslationKey> keys, string baseFileName, string language)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var currentKey = string.IsNullOrEmpty(prefix)
                    ? property.Name
                    : $"{prefix}.{property.Name}";

                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var rawValue = property.Value.GetString() ?? string.Empty;
                    var (actualValue, suggestion) = ParseValue(rawValue, language);
                    
                    var translationKey = new TranslationKey
                    {
                        Key = currentKey,
                        Source = new SourceFile(baseFileName, FileType.Json),
                        LanguageValues = new Dictionary<string, string>
                        {
                            { language, actualValue }
                        }
                    };
                    
                    if (suggestion != null)
                    {
                        translationKey.SuggestedValues[language] = suggestion;
                    }
                    
                    keys.Add(translationKey);
                }
                else if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    ParseJsonElement(property.Value, currentKey, keys, baseFileName, language);
                }
            }
        }
    }

    /// <summary>
    /// Extract the original JSON content for preserving structure on export
    /// </summary>
    public string ExtractTemplate(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return File.ReadAllText(filePath);
    }

    /// <summary>
    /// Validates that the JSON file name follows the expected naming convention.
    /// Expected pattern: {BaseName}.{language}.json (e.g., Forms.en.json, Forms.es.json)
    /// </summary>
    /// <param name="fileName">The file name to validate (without path)</param>
    /// <returns>True if the file name matches the expected pattern, otherwise false</returns>
    public bool ValidateNamingConvention(string fileName)
    {
        // Pattern: {BaseName}.{language}.json where language is 2-letter code
        var pattern = new Regex(@"^[A-Za-z0-9_-]+\.(en|es|fr|de|it|pt|ru|zh|ja|ko|ar)\.json$", RegexOptions.IgnoreCase);
        return pattern.IsMatch(fileName);
    }
}
