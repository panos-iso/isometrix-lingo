using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.Services;

public class JsonTranslationFileWriter
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Writes translation keys to JSON files, grouped by language and source file
    /// </summary>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Group keys by source file
        var groupedByFile = keys.GroupBy(k => k.Source.Name);

        foreach (var fileGroup in groupedByFile)
        {
            var sourceFile = fileGroup.Key;
            var fileKeys = fileGroup.ToList();

            // Get all languages for this file
            var languages = fileKeys
                .SelectMany(k => k.LanguageValues.Keys)
                .Distinct()
                .ToList();

            // Write a file for each language
            foreach (var language in languages)
            {
                var fileName = $"{sourceFile}_{language}.json";
                var filePath = Path.Combine(outputDirectory, fileName);

                WriteLanguageFile(filePath, fileKeys, language);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language)
    {
        var jsonObject = new Dictionary<string, object>();

        foreach (var key in keys)
        {
            if (key.LanguageValues.TryGetValue(language, out var value))
            {
                SetNestedValue(jsonObject, key.Key, value);
            }
        }

        var json = JsonSerializer.Serialize(jsonObject, _options);
        File.WriteAllText(filePath, json);
    }

    private void SetNestedValue(Dictionary<string, object> root, string key, string value)
    {
        var parts = key.Split('.');
        var current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            
            if (!current.ContainsKey(part))
            {
                current[part] = new Dictionary<string, object>();
            }

            if (current[part] is Dictionary<string, object> dict)
            {
                current = dict;
            }
            else
            {
                // Handle case where path conflicts (shouldn't happen with valid data)
                return;
            }
        }

        current[parts[^1]] = value;
    }
}
