using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

/// <summary>
/// Service for deploying exported translation files back to their original repository locations.
/// </summary>
public class DeploymentService
{
    /// <summary>
    /// Suggests a deployment root directory by searching parent/siblings for a directory matching the ZIP prefix.
    /// </summary>
    /// <param name="exportDirectory">The directory where the ZIP was exported</param>
    /// <param name="zipFileName">The name of the exported ZIP file (e.g., "iso_exported_20260529_120000.zip")</param>
    /// <returns>Suggested deployment root path, or empty string if no match found</returns>
    public string SuggestDeploymentRoot(string exportDirectory, string zipFileName)
    {
        try
        {
            // Extract the prefix from the ZIP filename (everything before "_exported_")
            var prefix = ExtractZipPrefix(zipFileName);
            if (string.IsNullOrEmpty(prefix))
            {
                return string.Empty;
            }

            // Get parent directory (one level up)
            var parentDir = Directory.GetParent(exportDirectory);
            if (parentDir == null || !parentDir.Exists)
            {
                return string.Empty;
            }

            // Search for matching directory in parent (siblings of export directory)
            var siblingDirs = Directory.GetDirectories(parentDir.FullName);
            foreach (var siblingPath in siblingDirs)
            {
                var dirName = Path.GetFileName(siblingPath);
                if (string.Equals(dirName, prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return siblingPath;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Validates that the deployment root directory name matches the ZIP prefix (soft validation).
    /// </summary>
    /// <param name="deploymentRoot">The selected deployment root directory</param>
    /// <param name="zipFileName">The name of the exported ZIP file</param>
    /// <returns>Validation result with warning message if mismatch detected</returns>
    public (bool IsMatch, string Message) ValidateRootNameMatch(string deploymentRoot, string zipFileName)
    {
        try
        {
            var prefix = ExtractZipPrefix(zipFileName);
            if (string.IsNullOrEmpty(prefix))
            {
                return (false, "Unable to extract prefix from ZIP filename.");
            }

            var rootDirName = Path.GetFileName(deploymentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            
            if (string.Equals(rootDirName, prefix, StringComparison.OrdinalIgnoreCase))
            {
                return (true, $"Root directory '{rootDirName}' matches ZIP prefix '{prefix}'.");
            }
            else
            {
                return (false, $"⚠️ Warning: Root directory '{rootDirName}' does not match ZIP prefix '{prefix}'. Files may be deployed to the wrong location.");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that ALL file paths in the ZIP can be mapped to valid deployment locations (hard validation).
    /// </summary>
    /// <param name="zipFilePath">Full path to the exported ZIP file</param>
    /// <param name="deploymentRoot">The deployment root directory</param>
    /// <returns>List of validation errors (empty if all paths are valid)</returns>
    public List<ImportError> ValidateAllPaths(string zipFilePath, string deploymentRoot)
    {
        var errors = new List<ImportError>();

        try
        {
            if (!File.Exists(zipFilePath))
            {
                errors.Add(new ImportError
                {
                    ErrorType = ImportErrorType.FileNotFound,
                    FileName = zipFilePath,
                    Message = "ZIP file not found."
                });
                return errors;
            }

            if (!Directory.Exists(deploymentRoot))
            {
                errors.Add(new ImportError
                {
                    ErrorType = ImportErrorType.InvalidPath,
                    FileName = deploymentRoot,
                    Message = "Deployment root directory does not exist."
                });
                return errors;
            }

            using var archive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                // Map to deployment target path
                var targetPath = Path.Combine(deploymentRoot, entry.FullName);

                // Validate target directory can be created
                var targetDir = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrEmpty(targetDir))
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.InvalidPath,
                        FileName = entry.FullName,
                        Message = $"Cannot determine target directory for '{entry.FullName}'."
                    });
                    continue;
                }

                // Check if path is valid (not trying to escape deployment root)
                var fullTargetPath = Path.GetFullPath(targetPath);
                var fullDeploymentRoot = Path.GetFullPath(deploymentRoot);
                
                if (!fullTargetPath.StartsWith(fullDeploymentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.InvalidPath,
                        FileName = entry.FullName,
                        Message = $"File '{entry.FullName}' would be deployed outside the deployment root. This is not allowed."
                    });
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ImportError
            {
                ErrorType = ImportErrorType.ReadError,
                FileName = zipFilePath,
                Message = $"Failed to validate ZIP contents: {ex.Message}"
            });
        }

        return errors;
    }

    /// <summary>
    /// Generates a deployment preview showing source → target file mappings.
    /// </summary>
    /// <param name="zipFilePath">Full path to the exported ZIP file</param>
    /// <param name="deploymentRoot">The deployment root directory</param>
    /// <returns>List of deployment preview items</returns>
    public List<DeploymentPreviewItem> GetDeploymentPreview(string zipFilePath, string deploymentRoot)
    {
        var previewItems = new List<DeploymentPreviewItem>();

        try
        {
            if (!File.Exists(zipFilePath))
            {
                return previewItems;
            }

            using var archive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var targetPath = Path.Combine(deploymentRoot, entry.FullName);

                previewItems.Add(new DeploymentPreviewItem
                {
                    SourcePath = entry.FullName,
                    TargetPath = targetPath,
                    IsValid = true
                });
            }
        }
        catch (Exception ex)
        {
            previewItems.Add(new DeploymentPreviewItem
            {
                SourcePath = "ERROR",
                TargetPath = ex.Message,
                IsValid = false,
                ErrorMessage = $"Failed to read ZIP contents: {ex.Message}"
            });
        }

        return previewItems;
    }

    /// <summary>
    /// Executes deployment by copying all files from the ZIP to the deployment root (all-or-nothing).
    /// </summary>
    /// <param name="zipFilePath">Full path to the exported ZIP file</param>
    /// <param name="deploymentRoot">The deployment root directory</param>
    /// <returns>Success status and list of errors if any</returns>
    public (bool Success, List<ImportError> Errors) ExecuteDeployment(string zipFilePath, string deploymentRoot)
    {
        var errors = new List<ImportError>();
        var deployedFiles = new List<string>();

        try
        {
            // First, validate all paths (hard validation)
            errors = ValidateAllPaths(zipFilePath, deploymentRoot);
            if (errors.Count > 0)
            {
                return (false, errors);
            }

            // Extract and deploy files
            using var archive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var targetPath = Path.Combine(deploymentRoot, entry.FullName);

                try
                {
                    // Ensure target directory exists
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // Extract file to target (overwrite if exists)
                    entry.ExtractToFile(targetPath, overwrite: true);
                    deployedFiles.Add(targetPath);
                }
                catch (Exception ex)
                {
                    // If ANY file fails, rollback all deployed files and abort
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.WriteError,
                        FileName = entry.FullName,
                        Message = $"Failed to deploy '{entry.FullName}': {ex.Message}"
                    });

                    // Rollback: attempt to delete all successfully deployed files
                    RollbackDeployment(deployedFiles);

                    return (false, errors);
                }
            }

            return (true, errors);
        }
        catch (Exception ex)
        {
            errors.Add(new ImportError
            {
                ErrorType = ImportErrorType.ReadError,
                FileName = zipFilePath,
                Message = $"Deployment failed: {ex.Message}"
            });

            // Rollback any deployed files
            RollbackDeployment(deployedFiles);

            return (false, errors);
        }
    }

    /// <summary>
    /// Extracts the prefix from a ZIP filename (everything before "_exported_").
    /// </summary>
    /// <param name="zipFileName">ZIP filename (e.g., "iso_exported_20260529_120000.zip")</param>
    /// <returns>Prefix string (e.g., "iso"), or empty if not found</returns>
    private string ExtractZipPrefix(string zipFileName)
    {
        try
        {
            // Remove .zip extension
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(zipFileName);

            // Find "_exported_" marker
            var marker = "_exported_";
            var index = nameWithoutExtension.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            
            if (index > 0)
            {
                return nameWithoutExtension.Substring(0, index);
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Attempts to rollback a deployment by deleting all deployed files.
    /// </summary>
    /// <param name="deployedFiles">List of file paths that were successfully deployed</param>
    private void RollbackDeployment(List<string> deployedFiles)
    {
        foreach (var filePath in deployedFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Best effort rollback - ignore errors
            }
        }
    }
}
