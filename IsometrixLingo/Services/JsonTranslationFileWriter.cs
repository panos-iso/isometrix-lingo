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
