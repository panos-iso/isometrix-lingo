using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class ResxTranslationFileReader
{
    private readonly Regex _languagePattern = new(@"[_\.]([a-z]{2})\.resx$", RegexOptions.IgnoreCase);

    public TranslationFile ReadFile(string filePath)
    {
        var language = ExtractLanguage(filePath);
        var baseFileName = ExtractBaseFileName(filePath);
        var keys = new List<TranslationKey>();

        // Load RESX file as XML
        var xdoc = XDocument.Load(filePath);

        // Query the <data> elements
        var resources = xdoc.Descendants("data")
            .Where(data => data.Attribute("name") != null && data.Element("value") != null)
            .Select(data => new
            {
                Name = data.Attribute("name")!.Value,
                Value = data.Element("value")!.Value
            });

        foreach (var resource in resources)
        {
            var rawValue = resource.Value;
            var (actualValue, suggestion) = ParseValueWithSuggestion(rawValue, language);
            
            var translationKey = new TranslationKey
            {
                Key = resource.Name,
                Source = new SourceFile(baseFileName, FileType.Resx),
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

        return new TranslationFile
        {
            FilePath = filePath,
            Language = language,
            FileType = FileType.Resx,
            Keys = keys
        };
    }

    public string ExtractLanguage(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var match = _languagePattern.Match(fileName);

        if (match.Success)
        {
            return match.Groups[1].Value.ToLower();
        }

        // If no language code found, assume it's the default/English file
        if (fileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        throw new ArgumentException($"Could not extract language code from file name: {fileName}");
    }

    public string ExtractBaseFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var match = _languagePattern.Match(Path.GetFileName(filePath));

        if (match.Success)
        {
            // Remove language suffix (e.g., "FormTranslations_es" → "FormTranslations")
            return fileName.Substring(0, fileName.Length - 3);
        }

        // No language suffix found, this is the base file (e.g., "FormTranslations.resx")
        return fileName;
    }

    /// <summary>
    /// Consolidate keys from multiple RESX files into a unified list
    /// </summary>
    public List<TranslationKey> ConsolidateKeys(List<TranslationFile> files)
    {
        var consolidatedKeys = new Dictionary<string, TranslationKey>();

        foreach (var file in files)
        {
            foreach (var key in file.Keys)
            {
                if (consolidatedKeys.TryGetValue(key.Key, out var existingKey))
                {
                    // Merge language values
                    foreach (var langValue in key.LanguageValues)
                    {
                        existingKey.LanguageValues[langValue.Key] = langValue.Value;
                    }
                }
                else
                {
                    consolidatedKeys[key.Key] = new TranslationKey
                    {
                        Key = key.Key,
                        Source = key.Source,
                        LanguageValues = new Dictionary<string, string>(key.LanguageValues)
                    };
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

        return consolidatedKeys.Values.OrderBy(k => k.Key).ToList();
    }

    /// <summary>
    /// Parse a value string that may contain a suggestion
    /// Format: "actual value SUGGESTION:suggested_value,by:[username],at:[datetime]"
    /// Returns the actual value and the parsed suggestion (if present)
    /// </summary>
    private (string actualValue, Suggestion? suggestion) ParseValueWithSuggestion(string rawValue, string language)
    {
        const string suggestionPrefix = " SUGGESTION:";
        var suggestionIndex = rawValue.IndexOf(suggestionPrefix, StringComparison.Ordinal);
        
        if (suggestionIndex == -1)
        {
            // No suggestion in this value
            return (rawValue, null);
        }

        // Split actual value and suggestion
        var actualValue = rawValue.Substring(0, suggestionIndex);
        var suggestionPart = rawValue.Substring(suggestionIndex + 1); // Skip the leading space
        
        var suggestion = Suggestion.FromFileFormat(suggestionPart);
        return (actualValue, suggestion);
    }

    /// <summary>
    /// Extract the RESX template preserving all structure including data elements
    /// </summary>
    public XDocument ExtractTemplate(string filePath)
    {
        var xdoc = XDocument.Load(filePath);

        // Return the entire document - we'll update data elements in place during export
        return new XDocument(xdoc);
    }
}

