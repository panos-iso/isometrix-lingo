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
    /// <param name="currentMode">Current workflow mode (Edit/Suggest/Deployment)</param>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory, Func<string, string?>? templateProvider = null, string? username = null, EditMode currentMode = EditMode.Edit)
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

                WriteLanguageFile(filePath, fileKeys, language, template, username, currentMode);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language, string? template, string? username, EditMode currentMode)
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
                    // Get actual value
                    var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                    
                    // In Deployment mode, write clean values without annotations
                    // In Edit/Suggest modes, append annotations inline (JSON doesn't support comments natively)
                    var finalValue = currentMode == EditMode.Deployment 
                        ? value 
                        : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);
                    
                    if (UpdateNestedValue(jsonObject, key.Key, finalValue))
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
                        
                        // In Deployment mode, write clean values without annotations
                        var finalValue = currentMode == EditMode.Deployment 
                            ? value 
                            : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);
                        
                        SetNestedValue(jsonObject, key.Key, finalValue);
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
                    
                    // In Deployment mode, write clean values without annotations
                    var finalValue = currentMode == EditMode.Deployment 
                        ? value 
                        : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);
                    
                    SetNestedValue(jsonObject, key.Key, finalValue);
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
                
                // In Deployment mode, write clean values without annotations  
                var finalValue = currentMode == EditMode.Deployment 
                    ? value 
                    : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);
                
                SetNestedValue(jsonObject, key.Key, finalValue);
            }
        }

        var json = jsonObject.ToJsonString(_options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Append suggestion annotation to value (no confirmations)
    /// Format: "actual value iso-lingo-audit:SUGGESTION:...,by:[username],at:[datetime]"
    /// </summary>
    private string AppendAnnotations(string actualValue, TranslationKey key, string language, string? username, bool isEditMode)
    {
        var result = actualValue;
        
        // Append suggestion if exists for this language
        if (key.SuggestedValues.TryGetValue(language, out var suggestion))
        {
            result = $"{result} {suggestion.ToFileFormat()}";
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
