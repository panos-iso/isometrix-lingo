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
            var (actualValue, suggestion, confirmation) = ParseValue(rawValue, language);
            
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
            
            // Confirmation is at key level, store in base file (English)
            if (confirmation != null && language == "en")
            {
                translationKey.ConfirmedBy = confirmation;
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
        var directoryPath = files.FirstOrDefault()?.RelativeDirectoryPath;

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
                    
                    // Merge suggestions
                    foreach (var suggestion in key.SuggestedValues)
                    {
                        existingKey.SuggestedValues[suggestion.Key] = suggestion.Value;
                    }
                    
                    // Merge confirmation (from base file)
                    if (key.ConfirmedBy != null && existingKey.ConfirmedBy == null)
                    {
                        existingKey.ConfirmedBy = key.ConfirmedBy;
                    }
                }
                else
                {
                    // Create new key with updated Source including directory path
                    consolidatedKeys[key.Key] = new TranslationKey
                    {
                        Key = key.Key,
                        Source = new SourceFile(key.Source.Name, key.Source.Type, directoryPath),
                        LanguageValues = new Dictionary<string, string>(key.LanguageValues),
                        SuggestedValues = new Dictionary<string, Suggestion>(key.SuggestedValues),
                        ConfirmedBy = key.ConfirmedBy
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

    /// <summary>
    /// Extract the RESX template preserving all structure including data elements
    /// </summary>
    public XDocument ExtractTemplate(string filePath)
    {
        var xdoc = XDocument.Load(filePath);

        // Return the entire document - we'll update data elements in place during export
        return new XDocument(xdoc);
    }

    /// <summary>
    /// Validates that the RESX file name follows the expected naming convention.
    /// Expected patterns:
    /// - English base: {BaseName}.resx (e.g., FormTranslations.resx)
    /// - Localized: {BaseName}_{language}.resx (e.g., FormTranslations_es.resx)
    /// </summary>
    /// <param name="fileName">The file name to validate (without path)</param>
    /// <returns>True if the file name matches the expected pattern, otherwise false</returns>
    public bool ValidateNamingConvention(string fileName)
    {
        // Pattern: {BaseName}.resx or {BaseName}_{language}.resx where language is 2-letter code
        var pattern = new Regex(@"^[A-Za-z0-9_-]+(_(?:en|es|fr|de|it|pt|ru|zh|ja|ko|ar))?\.resx$", RegexOptions.IgnoreCase);
        return pattern.IsMatch(fileName);
    }
}

