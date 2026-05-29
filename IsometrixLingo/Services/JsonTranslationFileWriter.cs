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
        // ALWAYS write flat JSON - no nested objects
        // Keys with dots stay as literal keys: "buttons.activate" not { "buttons": { "activate": ... }}
        // PRESERVE ORIGINAL KEY ORDER - JsonObject maintains insertion order in .NET 5+
        var jsonObject = new JsonObject();
        
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

        var json = jsonObject.ToJsonString(_options);
        
        // Preserve trailing newlines from original file
        string? trailingWhitespace = null;
        
        // First check template if available
        if (!string.IsNullOrEmpty(template))
        {
            var templateTrailing = template.Length - template.TrimEnd('\r', '\n').Length;
            if (templateTrailing > 0)
            {
                trailingWhitespace = template.Substring(template.Length - templateTrailing);
            }
        }
        // Otherwise check existing file at target location
        else if (File.Exists(filePath))
        {
            var originalContent = File.ReadAllText(filePath);
            var trailingNewlines = originalContent.Length - originalContent.TrimEnd('\r', '\n').Length;
            if (trailingNewlines > 0)
            {
                trailingWhitespace = originalContent.Substring(originalContent.Length - trailingNewlines);
            }
        }
        
        if (trailingWhitespace != null)
        {
            json += trailingWhitespace;
        }
        
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
}
