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
        var consolidatedKeys = new Dictionary<string, TranslationKey>();
        var baseFileName = files.FirstOrDefault()?.FilePath;
        var baseName = baseFileName != null ? ExtractBaseFileName(baseFileName) : "unknown";

        // Collect all unique keys across all files
        foreach (var file in files)
        {
            foreach (var key in file.Keys)
            {
                if (!consolidatedKeys.ContainsKey(key.Key))
                {
                    consolidatedKeys[key.Key] = new TranslationKey
                    {
                        Key = key.Key,
                        Source = new SourceFile(baseName, FileType.Json),
                        LanguageValues = new Dictionary<string, string>(),
                        SuggestedValues = new Dictionary<string, Suggestion>()
                    };
                }

                // Add language value
                if (key.LanguageValues.ContainsKey(file.Language))
                {
                    consolidatedKeys[key.Key].LanguageValues[file.Language] = key.LanguageValues[file.Language];
                }
                
                // Add suggestion if present
                if (key.SuggestedValues.ContainsKey(file.Language))
                {
                    consolidatedKeys[key.Key].SuggestedValues[file.Language] = key.SuggestedValues[file.Language];
                }
                
                // Add confirmation if present (from English file)
                if (key.ConfirmedBy != null && consolidatedKeys[key.Key].ConfirmedBy == null)
                {
                    consolidatedKeys[key.Key].ConfirmedBy = key.ConfirmedBy;
                }
            }
        }

        // Fill in missing language values with empty strings
        var allLanguages = files.Select(f => f.Language).Distinct().ToList();
        foreach (var translationKey in consolidatedKeys.Values)
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

        return consolidatedKeys.Values.ToList();
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
    /// Parse a value string that may contain a suggestion and/or confirmation
    /// Format: "actual value SUGGESTION:suggested_value,by:[username],at:[datetime] CONFIRMED:by:[username],at:[datetime]"
    /// Returns the actual value, parsed suggestion (if present), and parsed confirmation (if present)
    /// </summary>
    private (string actualValue, Suggestion? suggestion, Confirmation? confirmation) ParseValue(string rawValue, string language)
    {
        const string suggestionPrefix = " SUGGESTION:";
        const string confirmedPrefix = " CONFIRMED:";
        
        var actualValue = rawValue;
        Suggestion? suggestion = null;
        Confirmation? confirmation = null;
        
        // Check for SUGGESTION
        var suggestionIndex = rawValue.IndexOf(suggestionPrefix, StringComparison.Ordinal);
        if (suggestionIndex != -1)
        {
            actualValue = rawValue.Substring(0, suggestionIndex);
            var remainingAfterValue = rawValue.Substring(suggestionIndex + 1); // Skip the leading space
            
            // Find where suggestion ends (either at CONFIRMED or end of string)
            var confirmedIndexInRemaining = remainingAfterValue.IndexOf(confirmedPrefix, StringComparison.Ordinal);
            
            string suggestionPart;
            if (confirmedIndexInRemaining != -1)
            {
                suggestionPart = remainingAfterValue.Substring(0, confirmedIndexInRemaining);
            }
            else
            {
                suggestionPart = remainingAfterValue;
            }
            
            suggestion = Suggestion.FromFileFormat(suggestionPart);
        }
        
        // Check for CONFIRMED
        var confirmedIndex = rawValue.IndexOf(confirmedPrefix, StringComparison.Ordinal);
        if (confirmedIndex != -1)
        {
            // If there's no suggestion, extract actual value up to CONFIRMED
            if (suggestionIndex == -1)
            {
                actualValue = rawValue.Substring(0, confirmedIndex);
            }
            
            var confirmedPart = rawValue.Substring(confirmedIndex + 1); // Skip the leading space
            confirmation = Confirmation.FromFileFormat(confirmedPart);
        }
        
        return (actualValue, suggestion, confirmation);
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
                    var (actualValue, suggestion, confirmation) = ParseValue(rawValue, language);
                    
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
                    
                    // Confirmation is at key level, only parse from English file
                    if (confirmation != null && language == "en")
                    {
                        translationKey.ConfirmedBy = confirmation;
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
