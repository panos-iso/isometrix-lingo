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
                        LanguageValues = new Dictionary<string, string>()
                    };
                }

                // Add language value
                if (key.LanguageValues.ContainsKey(file.Language))
                {
                    consolidatedKeys[key.Key].LanguageValues[file.Language] = key.LanguageValues[file.Language];
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
                    keys.Add(new TranslationKey
                    {
                        Key = currentKey,
                        Source = new SourceFile(baseFileName, FileType.Json),
                        LanguageValues = new Dictionary<string, string>
                        {
                            { language, property.Value.GetString() ?? string.Empty }
                        }
                    });
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
}
