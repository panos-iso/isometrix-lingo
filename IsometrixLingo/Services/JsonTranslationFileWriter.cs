using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

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
    /// <param name="username">Username for confirmation auditing (optional)</param>
    /// <param name="isEditMode">Whether in Edit mode (confirmations only created/updated in Edit mode)</param>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory, Func<string, string?>? templateProvider = null, string? username = null, bool isEditMode = true)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Group keys by source file (including directory path)
        var groupedByFile = keys.GroupBy(k => k.Source);

        foreach (var fileGroup in groupedByFile)
        {
            var source = fileGroup.Key;
            var fileKeys = fileGroup.ToList();

            // Determine output directory based on DirectoryPath
            var targetDirectory = outputDirectory;
            if (!string.IsNullOrEmpty(source.DirectoryPath))
            {
                targetDirectory = Path.Combine(outputDirectory, source.DirectoryPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
            }

            // Get all languages for this file
            var languages = fileKeys
                .SelectMany(k => k.LanguageValues.Keys)
                .Distinct()
                .ToList();

            // Write a file for each language
            foreach (var language in languages)
            {
                var fileName = $"{source.Name}.{language}.json";
                var filePath = Path.Combine(targetDirectory, fileName);

                // Get template for this source file if available
                var template = templateProvider?.Invoke(source.Name);

                WriteLanguageFile(filePath, fileKeys, language, template, username, isEditMode);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language, string? template, string? username, bool isEditMode)
    {
        JsonObject jsonObject;

        if (!string.IsNullOrEmpty(template))
        {
            // Parse the template to preserve structure and order
            try
            {
                var parsedNode = JsonNode.Parse(template);
                jsonObject = parsedNode as JsonObject ?? new JsonObject();

                // Update existing values and track processed keys
                var processedKeys = new HashSet<string>();

                foreach (var key in keys)
                {
                    // Get actual value and suggestion
                    var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                    var fullValue = AppendAnnotations(value, key, language, username, isEditMode);
                    
                    if (UpdateNestedValue(jsonObject, key.Key, fullValue))
                    {
                        processedKeys.Add(key.Key);
                    }
                }

                // Add new keys that weren't in the template
                foreach (var key in keys)
                {
                    if (!processedKeys.Contains(key.Key))
                    {
                        var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                        var fullValue = AppendAnnotations(value, key, language, username, isEditMode);
                        SetNestedValue(jsonObject, key.Key, fullValue);
                    }
                }
            }
            catch (JsonException)
            {
                // If template parsing fails, fall back to creating from scratch
                jsonObject = new JsonObject();
                foreach (var key in keys)
                {
                    var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                    var fullValue = AppendAnnotations(value, key, language, username, isEditMode);
                    SetNestedValue(jsonObject, key.Key, fullValue);
                }
            }
        }
        else
        {
            // No template - create from scratch
            jsonObject = new JsonObject();
            foreach (var key in keys)
            {
                var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                var fullValue = AppendAnnotations(value, key, language, username, isEditMode);
                SetNestedValue(jsonObject, key.Key, fullValue);
            }
        }

        var json = jsonObject.ToJsonString(_options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Append suggestion and confirmation annotations to value
    /// Format: "actual value SUGGESTION:...,by:[username],at:[datetime] CONFIRMED:by:[username],at:[datetime]"
    /// Auto-creates/updates confirmation for keys with both en and es values when writing English files (Edit mode only)
    /// </summary>
    private string AppendAnnotations(string actualValue, TranslationKey key, string language, string? username, bool isEditMode)
    {
        var result = actualValue;
        
        // Append suggestion if exists for this language
        if (key.SuggestedValues.TryGetValue(language, out var suggestion))
        {
            result = $"{result} {suggestion.ToFileFormat()}";
        }
        
        // For English files, append confirmation (auto-create/update/remove only in Edit mode)
        if (language == "en")
        {
            if (isEditMode)
            {
                var hasEnglish = key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
                var hasSpanish = key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
                
                // If key has both languages, ensure it has confirmation
                if (hasEnglish && hasSpanish)
                {
                    // If key was edited, override confirmation with new one
                    if (key.IsModified && !string.IsNullOrWhiteSpace(username))
                    {
                        key.ConfirmedBy = new Confirmation
                        {
                            Username = username,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                    // If key was not edited and has no confirmation, create one
                    else if (!key.IsModified && key.ConfirmedBy == null && !string.IsNullOrWhiteSpace(username))
                    {
                        key.ConfirmedBy = new Confirmation
                        {
                            Username = username,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                    // Otherwise keep existing confirmation (if any)
                }
                // If key is incomplete, remove any existing confirmation
                else if (key.ConfirmedBy != null)
                {
                    key.ConfirmedBy = null;
                }
            }
            
            // Always write existing confirmations to file (both Edit and Suggest mode)
            if (key.ConfirmedBy != null)
            {
                result = $"{result} {key.ConfirmedBy.ToFileFormat()}";
            }
        }
        
        return result;
    }

    /// <summary>
    /// Update a value in an existing nested structure, returns true if key was found and updated
    /// </summary>
    private bool UpdateNestedValue(JsonObject root, string key, string value)
    {
        var parts = key.Split('.');
        JsonObject current = root;

        // Navigate to the parent of the target key
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];

            if (!current.ContainsKey(part))
            {
                return false; // Path doesn't exist in template
            }

            if (current[part] is JsonObject jsonObj)
            {
                current = jsonObj;
            }
            else
            {
                return false; // Path conflicts with non-object value
            }
        }

        var finalKey = parts[^1];
        if (current.ContainsKey(finalKey))
        {
            current[finalKey] = JsonValue.Create(value);
            return true;
        }

        return false;
    }

    private void SetNestedValue(JsonObject root, string key, string value)
    {
        var parts = key.Split('.');
        JsonObject current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];

            if (!current.ContainsKey(part))
            {
                current[part] = new JsonObject();
            }

            if (current[part] is JsonObject jsonObj)
            {
                current = jsonObj;
            }
            else
            {
                // Handle case where path conflicts (shouldn't happen with valid data)
                return;
            }
        }

        current[parts[^1]] = JsonValue.Create(value);
    }
}
