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
    /// <param name="keys">Translation keys to write</param>
    /// <param name="outputDirectory">Output directory for files</param>
    /// <param name="templateProvider">Optional function to provide JSON template for a given source file name</param>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory, Func<string, string?>? templateProvider = null)
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
                var fileName = $"{sourceFile}.{language}.json";
                var filePath = Path.Combine(outputDirectory, fileName);

                // Get template for this source file if available
                var template = templateProvider?.Invoke(sourceFile);

                WriteLanguageFile(filePath, fileKeys, language, template);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language, string? template)
    {
        Dictionary<string, object> jsonObject;

        if (!string.IsNullOrEmpty(template))
        {
            // Parse the template to preserve structure and order
            try
            {
                jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(template, _options)
                    ?? new Dictionary<string, object>();

                // Update existing values and track processed keys
                var processedKeys = new HashSet<string>();

                foreach (var key in keys)
                {
                    if (key.LanguageValues.TryGetValue(language, out var value))
                    {
                        if (UpdateNestedValue(jsonObject, key.Key, value))
                        {
                            processedKeys.Add(key.Key);
                        }
                    }
                }

                // Add new keys that weren't in the template
                foreach (var key in keys)
                {
                    if (!processedKeys.Contains(key.Key) && key.LanguageValues.TryGetValue(language, out var value))
                    {
                        SetNestedValue(jsonObject, key.Key, value);
                    }
                }
            }
            catch (JsonException)
            {
                // If template parsing fails, fall back to creating from scratch
                jsonObject = new Dictionary<string, object>();
                foreach (var key in keys)
                {
                    if (key.LanguageValues.TryGetValue(language, out var value))
                    {
                        SetNestedValue(jsonObject, key.Key, value);
                    }
                }
            }
        }
        else
        {
            // No template - create from scratch
            jsonObject = new Dictionary<string, object>();
            foreach (var key in keys)
            {
                if (key.LanguageValues.TryGetValue(language, out var value))
                {
                    SetNestedValue(jsonObject, key.Key, value);
                }
            }
        }

        var json = JsonSerializer.Serialize(jsonObject, _options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Update a value in an existing nested structure, returns true if key was found and updated
    /// </summary>
    private bool UpdateNestedValue(Dictionary<string, object> root, string key, string value)
    {
        var parts = key.Split('.');
        var current = root;

        // Navigate to the parent of the target key
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];

            if (!current.ContainsKey(part))
            {
                return false; // Path doesn't exist in template
            }

            if (current[part] is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Convert JsonElement to Dictionary for modification
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText(), _options)
                    ?? new Dictionary<string, object>();
                current[part] = dict;
                current = dict;
            }
            else if (current[part] is Dictionary<string, object> dict)
            {
                current = dict;
            }
            else
            {
                return false; // Path conflicts with non-object value
            }
        }

        var finalKey = parts[^1];
        if (current.ContainsKey(finalKey))
        {
            current[finalKey] = value;
            return true;
        }

        return false;
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
