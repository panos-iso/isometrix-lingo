using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

/// <summary>
/// Service to scan directories for translation files (.json and .resx)
/// </summary>
public class DirectoryScanner
{
    private readonly JsonTranslationFileReader _jsonReader;
    private readonly ResxTranslationFileReader _resxReader;

    public DirectoryScanner(JsonTranslationFileReader jsonReader, ResxTranslationFileReader resxReader)
    {
        _jsonReader = jsonReader;
        _resxReader = resxReader;
    }

    /// <summary>
    /// Scans immediate subdirectories of the parent path
    /// Shows all subdirectories so user can exclude personal projects
    /// </summary>
    /// <param name="parentPath">Parent directory containing repository folders</param>
    /// <returns>List of scan results, one per subdirectory</returns>
    public List<DirectoryScanResult> ScanDirectory(string parentPath)
    {
        var results = new List<DirectoryScanResult>();

        if (!Directory.Exists(parentPath))
        {
            return results;
        }

        try
        {
            // Get immediate subdirectories (each represents a potential repository)
            var subdirectories = Directory.GetDirectories(parentPath);

            foreach (var subdirectory in subdirectories)
            {
                var directoryName = Path.GetFileName(subdirectory);
                
                // Skip common non-repo directories
                if (directoryName.StartsWith(".") || directoryName == "node_modules" || directoryName == "bin" || directoryName == "obj")
                {
                    continue;
                }

                // Recursively find all translation files in this subdirectory
                var translationFiles = FindTranslationFiles(subdirectory);

                // Show ALL subdirectories, even if no files (so user can see everything)
                results.Add(new DirectoryScanResult
                {
                    DirectoryPath = subdirectory,
                    DirectoryName = directoryName,
                    FileCount = translationFiles.Count,
                    TranslationFiles = translationFiles,
                    IsSelected = true // Default to selected
                });
            }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have permission to access
        }
        catch (Exception)
        {
            // Skip directories that cause other errors
        }

        return results.OrderBy(r => r.DirectoryName).ToList();
    }

    /// <summary>
    /// Recursively finds all translation files (.json and .resx) in a directory tree
    /// Only includes files that match valid naming conventions
    /// </summary>
    /// <param name="directoryPath">Root directory to search</param>
    /// <returns>List of file paths</returns>
    public List<string> FindTranslationFiles(string directoryPath)
    {
        var files = new List<string>();

        if (!Directory.Exists(directoryPath))
        {
            return files;
        }

        try
        {
            // Recursively search all subdirectories
            var allFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file).ToLower();
                var fileName = Path.GetFileName(file);

                // Check if it's a translation file type
                if (extension == ".json")
                {
                    // Validate JSON naming convention
                    if (_jsonReader.ValidateNamingConvention(fileName))
                    {
                        files.Add(file);
                    }
                }
                else if (extension == ".resx")
                {
                    // Validate RESX naming convention
                    if (_resxReader.ValidateNamingConvention(fileName))
                    {
                        files.Add(file);
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have permission to access
        }
        catch (Exception)
        {
            // Skip directories that cause other errors
        }

        return files;
    }
}
