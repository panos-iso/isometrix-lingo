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
        var dataElements = xdoc.Descendants("data")
            .Where(data => data.Attribute("name") != null && data.Element("value") != null);

        foreach (var dataElement in dataElements)
        {
            var keyName = dataElement.Attribute("name")!.Value;
            var actualValue = dataElement.Element("value")!.Value;
            Suggestion? suggestion = null;
            
            // Check for XML comments (iso-lingo-audit format)
            var comments = dataElement.Nodes().OfType<XComment>().ToList();
            foreach (var comment in comments)
            {
                var commentText = comment.Value.Trim();
                
                // Check if comment contains suggestion
                if (commentText.StartsWith("iso-lingo-audit:SUGGESTION:", StringComparison.Ordinal))
                {
                    suggestion = Suggestion.FromFileFormat(commentText);
                }
            }
            
            var translationKey = new TranslationKey
            {
                Key = keyName,
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

