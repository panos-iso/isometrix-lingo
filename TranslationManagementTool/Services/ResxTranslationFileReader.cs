using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.Services;

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
            keys.Add(new TranslationKey
            {
                Key = resource.Name,
                Source = new SourceFile(baseFileName, FileType.Resx),
                LanguageValues = new Dictionary<string, string>
                {
                    { language, resource.Value }
                }
            });
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

        throw new ArgumentException($"Could not extract language code from file name: {fileName}");
    }

    public string ExtractBaseFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var match = _languagePattern.Match(Path.GetFileName(filePath));
        
        if (match.Success)
        {
            return fileName.Substring(0, fileName.Length - 3);
        }

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

        return consolidatedKeys.Values.OrderBy(k => k.Key).ToList();
    }
}

