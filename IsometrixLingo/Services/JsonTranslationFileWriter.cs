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
    /// Copy original files and update them in-place with changes. Preserves ALL original file content.
    /// </summary>
    /// <param name="keys">Translation keys to export</param>
    /// <param name="sourceDirectory">Source directory containing original imported files</param>
    /// <param name="outputDirectory">Output directory for exported files</param>
    /// <param name="username">Username for auditing (optional)</param>
    /// <param name="currentMode">Current workflow mode (Edit/Suggest/Deployment)</param>
    public void CopyAndUpdateFiles(List<TranslationKey> keys, string sourceDirectory, string outputDirectory, string? username = null, EditMode currentMode = EditMode.Edit)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Group keys by source file
        var groupedByFile = keys.GroupBy(k => k.Source);

        foreach (var fileGroup in groupedByFile)
        {
            var source = fileGroup.Key;
            var fileKeys = fileGroup.ToList();

            // Get all languages for this file
            var languages = fileKeys
                .SelectMany(k => k.LanguageValues.Keys)
                .Distinct()
                .ToList();

            // Process each language file
            foreach (var language in languages)
            {
                var fileName = $"{source.Name}.{language}.json";
                
                // Build source and target paths
                var sourcePath = string.IsNullOrEmpty(source.DirectoryPath)
                    ? Path.Combine(sourceDirectory, fileName)
                    : Path.Combine(sourceDirectory, source.DirectoryPath, fileName);

                var targetDirectory = string.IsNullOrEmpty(source.DirectoryPath)
                    ? outputDirectory
                    : Path.Combine(outputDirectory, source.DirectoryPath);

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var targetPath = Path.Combine(targetDirectory, fileName);

                // Copy original file to preserve ALL content
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, targetPath, overwrite: true);
                }
                else
                {
                    // No original file - create new one (shouldn't happen in normal flow)
                    WriteLanguageFile(targetPath, fileKeys, language, null, username, currentMode);
                    continue;
                }

                // Update the copied file in-place
                UpdateJsonFileInPlace(targetPath, fileKeys, language, username, currentMode);
            }
        }
    }

    /// <summary>
    /// Update a JSON file in-place by modifying existing values and appending new keys at the end.
    /// </summary>
    private void UpdateJsonFileInPlace(string filePath, List<TranslationKey> keys, string language, string? username, EditMode currentMode)
    {
        var content = File.ReadAllText(filePath);
        var jsonNode = JsonNode.Parse(content);
        
        if (jsonNode is not JsonObject rootObject)
        {
            return;
        }

        // Detect if nested or flat structure
        bool isNested = false;
        foreach (var kvp in rootObject)
        {
            if (kvp.Value is JsonObject)
            {
                isNested = true;
                break;
            }
        }

        // Track which keys exist in original file
        var existingKeys = new HashSet<string>();
        CollectExistingKeys(rootObject, "", existingKeys, isNested);

        // Update existing values and collect new keys
        var newKeys = new List<TranslationKey>();
        
        foreach (var key in keys)
        {
            var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
            var finalValue = currentMode == EditMode.Deployment 
                ? value 
                : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);

            if (existingKeys.Contains(key.Key))
            {
                // Update existing key
                UpdateValueInJson(rootObject, key.Key, finalValue, isNested);
            }
            else
            {
                // New key - will append at end
                newKeys.Add(key);
            }
        }

        // Append new keys at the end
        foreach (var key in newKeys)
        {
            var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
            var finalValue = currentMode == EditMode.Deployment 
                ? value 
                : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);

            if (isNested)
            {
                // Add to nested structure
                AddToNestedStructure(rootObject, key.Key, finalValue);
            }
            else
            {
                // Add as flat key
                rootObject[key.Key] = JsonValue.Create(finalValue);
            }
        }

        // Write back preserving trailing newlines
        var json = rootObject.ToJsonString(_options);
        
        var trailingCount = content.Length - content.TrimEnd('\r', '\n').Length;
        if (trailingCount > 0)
        {
            json += content.Substring(content.Length - trailingCount);
        }
        
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Collect all existing keys from JSON (handles both flat and nested)
    /// </summary>
    private void CollectExistingKeys(JsonObject obj, string prefix, HashSet<string> keys, bool isNested)
    {
        foreach (var kvp in obj)
        {
            var fullKey = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
            
            if (kvp.Value is JsonObject nestedObj && isNested)
            {
                // Recurse into nested object
                CollectExistingKeys(nestedObj, fullKey, keys, isNested);
            }
            else
            {
                // Leaf value
                keys.Add(fullKey);
            }
        }
    }

    /// <summary>
    /// Update a value in JSON object (handles both flat and nested)
    /// </summary>
    private void UpdateValueInJson(JsonObject rootObject, string key, string value, bool isNested)
    {
        if (!isNested)
        {
            // Flat structure - direct update
            rootObject[key] = JsonValue.Create(value);
            return;
        }

        // Nested structure - navigate path
        var parts = key.Split('.');
        JsonObject current = rootObject;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (current[parts[i]] is JsonObject nestedObj)
            {
                current = nestedObj;
            }
            else
            {
                return; // Path doesn't exist (shouldn't happen)
            }
        }

        current[parts[^1]] = JsonValue.Create(value);
    }

    /// <summary>
    /// Add a new key to nested structure (creates intermediate objects if needed)
    /// </summary>
    private void AddToNestedStructure(JsonObject rootObject, string key, string value)
    {
        var parts = key.Split('.');
        
        if (parts.Length == 1)
        {
            rootObject[key] = JsonValue.Create(value);
            return;
        }

        JsonObject current = rootObject;
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            
            if (!current.ContainsKey(part))
            {
                current[part] = new JsonObject();
            }
            
            if (current[part] is JsonObject nestedObj)
            {
                current = nestedObj;
            }
            else
            {
                // Conflict - can't navigate further
                return;
            }
        }
        
        current[parts[^1]] = JsonValue.Create(value);
    }

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
        // Detect original structure from template or existing file
        string? originalContent = null;
        bool isNested = false;
        
        // First check template if available
        if (!string.IsNullOrEmpty(template))
        {
            originalContent = template;
            isNested = IsNestedStructure(template);
        }
        // Otherwise check existing file at target location
        else if (File.Exists(filePath))
        {
            originalContent = File.ReadAllText(filePath);
            isNested = IsNestedStructure(originalContent);
        }
        
        // Build JSON object in appropriate structure
        JsonObject jsonObject;
        
        if (isNested)
        {
            // Convert flat dot-notation keys to nested structure
            jsonObject = ConvertToNestedStructure(keys, language, username, currentMode);
        }
        else
        {
            // Write as flat key-value pairs
            jsonObject = new JsonObject();
            
            // Iterate keys in original order - NO SORTING
            foreach (var key in keys)
            {
                var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                
                // In Deployment mode, write clean values without annotations
                // In Edit/Suggest modes, append annotations inline
                var finalValue = currentMode == EditMode.Deployment 
                    ? value 
                    : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);
                
                // Write as flat key-value pair (no nesting)
                jsonObject[key.Key] = JsonValue.Create(finalValue);
            }
        }

        var json = jsonObject.ToJsonString(_options);
        
        // Preserve trailing newlines from original file
        string? trailingWhitespace = null;
        
        if (originalContent != null)
        {
            var trailingCount = originalContent.Length - originalContent.TrimEnd('\r', '\n').Length;
            if (trailingCount > 0)
            {
                trailingWhitespace = originalContent.Substring(originalContent.Length - trailingCount);
            }
        }
        
        if (trailingWhitespace != null)
        {
            json += trailingWhitespace;
        }
        
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Detect if JSON content has nested structure or is flat key-value pairs
    /// </summary>
    private bool IsNestedStructure(string jsonContent)
    {
        try
        {
            var jsonNode = JsonNode.Parse(jsonContent);
            if (jsonNode is not JsonObject jsonObject)
                return false;
            
            // If any top-level value is an object, it's nested
            foreach (var kvp in jsonObject)
            {
                if (kvp.Value is JsonObject)
                    return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Convert flat dot-notation keys to nested JSON structure
    /// Example: "config.dateFormat" -> { "config": { "dateFormat": "value" } }
    /// </summary>
    private JsonObject ConvertToNestedStructure(List<TranslationKey> keys, string language, string? username, EditMode currentMode)
    {
        var root = new JsonObject();
        
        // Process keys in original order to maintain sequence
        foreach (var key in keys)
        {
            var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
            
            // In Deployment mode, write clean values without annotations
            // In Edit/Suggest modes, append annotations inline
            var finalValue = currentMode == EditMode.Deployment 
                ? value 
                : AppendAnnotations(value, key, language, username, currentMode == EditMode.Edit);
            
            // Split key by dots and create nested structure
            var parts = key.Key.Split('.');
            
            if (parts.Length == 1)
            {
                // No nesting - direct key-value
                root[key.Key] = JsonValue.Create(finalValue);
            }
            else
            {
                // Navigate/create nested objects
                JsonObject current = root;
                
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    
                    if (!current.ContainsKey(part))
                    {
                        current[part] = new JsonObject();
                    }
                    
                    current = (JsonObject)current[part]!;
                }
                
                // Set the final value
                current[parts[^1]] = JsonValue.Create(finalValue);
            }
        }
        
        return root;
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
}
