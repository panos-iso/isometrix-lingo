using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class ResxTranslationFileWriter
{
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace Msdata = "urn:schemas-microsoft-com:xml-msdata";

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
                // For English, use base filename without language code; for others, add underscore + language
                var fileName = language == "en"
                    ? $"{source.Name}.resx"
                    : $"{source.Name}_{language}.resx";
                
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
                UpdateResxFileInPlace(targetPath, fileKeys, language, username, currentMode);
            }
        }
    }

    /// <summary>
    /// Update a RESX file in-place by modifying existing values and appending new keys at the end.
    /// PRESERVES original file's key order.
    /// </summary>
    private void UpdateResxFileInPlace(string filePath, List<TranslationKey> keys, string language, string? username, EditMode currentMode)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root;
        
        if (root == null)
        {
            return;
        }

        // Create lookup dictionary from keys list for fast access
        var keyLookup = keys.ToDictionary(k => k.Key, k => k);
        var processedKeys = new HashSet<string>();

        // Iterate through ORIGINAL file's data elements in their existing order
        var existingDataElements = root.Elements("data").ToList();
        
        foreach (var dataElement in existingDataElements)
        {
            var keyName = dataElement.Attribute("name")?.Value;
            if (keyName == null)
                continue;

            // If we have this key in our keys list, update it
            if (keyLookup.TryGetValue(keyName, out var translationKey))
            {
                var value = translationKey.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                
                // Update value element
                var valueElement = dataElement.Element("value");
                if (valueElement != null)
                {
                    valueElement.Value = value;
                }
                else
                {
                    dataElement.Add(new XElement("value", value));
                }
                
                // Update annotations (only in Edit and Suggest modes)
                if (currentMode != EditMode.Deployment)
                {
                    // Remove existing annotation comments
                    dataElement.Nodes().OfType<XComment>().ToList().ForEach(c => c.Remove());
                    AddAnnotationsAsComments(dataElement, translationKey, language, username, currentMode == EditMode.Edit);
                }

                processedKeys.Add(keyName);
            }
            // If key doesn't exist in our keys list, leave it unchanged (preserve original)
        }

        // Append NEW keys that weren't in the original file
        foreach (var key in keys)
        {
            if (!processedKeys.Contains(key.Key))
            {
                var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                
                var dataElement = new XElement("data",
                    new XAttribute("name", key.Key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value)
                );

                // Add annotations (only in Edit and Suggest modes)
                if (currentMode != EditMode.Deployment)
                {
                    AddAnnotationsAsComments(dataElement, key, language, username, currentMode == EditMode.Edit);
                }

                // Append to end
                root.Add(dataElement);
            }
        }

        // Save with XML declaration preserved
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = System.Text.Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using (var writer = System.Xml.XmlWriter.Create(filePath, settings))
        {
            doc.Save(writer);
        }
    }

    /// <summary>
    /// Writes translation keys to RESX files, grouped by language and source file
    /// </summary>
    /// <param name="keys">Translation keys to write</param>
    /// <param name="outputDirectory">Output directory for files</param>
    /// <param name="templateProvider">Optional function to provide RESX template for a given source file name</param>
    /// <param name="username">Username for auditing (optional)</param>
    /// <param name="currentMode">Current workflow mode (Edit/Suggest/Deployment)</param>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory, Func<string, XDocument?>? templateProvider = null, string? username = null, EditMode currentMode = EditMode.Edit)
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
                // For English, use base filename without language code; for others, add underscore + language
                var fileName = language == "en"
                    ? $"{source.Name}.resx"
                    : $"{source.Name}_{language}.resx";
                var filePath = Path.Combine(targetDirectory, fileName);

                // Get template for this source file if available
                var template = templateProvider?.Invoke(source.Name);

                WriteLanguageFile(filePath, fileKeys, language, template, username, currentMode);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language, XDocument? template, string? username, EditMode currentMode)
    {
        XDocument doc;

        if (template != null)
        {
            // Use the provided template (clone it to avoid modifying the original)
            // This preserves ALL original content including comments, declarations, etc.
            doc = new XDocument(template);
            var root = doc.Root ?? throw new InvalidOperationException("Template root is null");

            // Update existing data elements and track which keys we've processed
            var processedKeys = new HashSet<string>();
            var existingDataElements = root.Elements("data").ToList();

            foreach (var dataElement in existingDataElements)
            {
                var keyName = dataElement.Attribute("name")?.Value;
                if (keyName != null)
                {
                    // Find the translation key for this data element
                    var translationKey = keys.FirstOrDefault(k => k.Key == keyName);

                    if (translationKey != null)
                    {
                        // Get actual value
                        var value = translationKey.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                        
                        // Update the value in the existing data element
                        var valueElement = dataElement.Element("value");
                        if (valueElement != null)
                        {
                            valueElement.Value = value;
                        }
                        else
                        {
                            dataElement.Add(new XElement("value", value));
                        }
                        
                        // Add annotations as comments (only in Edit and Suggest modes)
                        if (currentMode != EditMode.Deployment)
                        {
                            // Remove existing comments from this data element
                            dataElement.Nodes().OfType<XComment>().ToList().ForEach(c => c.Remove());
                            AddAnnotationsAsComments(dataElement, translationKey, language, username, currentMode == EditMode.Edit);
                        }
                        
                        processedKeys.Add(keyName);
                    }
                }
            }

            // Add new data elements for keys that weren't in the template
            // These are APPENDED AT THE END of the file (not inserted in the middle)
            foreach (var key in keys)
            {
                if (!processedKeys.Contains(key.Key))
                {
                    var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                    
                    var dataElement = new XElement("data",
                        new XAttribute("name", key.Key),
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        new XElement("value", value)
                    );

                    // Add annotations as comments (only in Edit and Suggest modes)
                    if (currentMode != EditMode.Deployment)
                    {
                        AddAnnotationsAsComments(dataElement, key, language, username, currentMode == EditMode.Edit);
                    }

                    // Append to end of file
                    root.Add(dataElement);
                }
            }
        }
        else
        {
            // Fall back to creating a new RESX structure
            var root = CreateResxDocument();

            // Add data elements for each key in original order
            // Keys are already ordered correctly from consolidation
            foreach (var key in keys)
            {
                var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                
                // Create data element with clean value only
                var dataElement = new XElement("data",
                    new XAttribute("name", key.Key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value)
                );

                // Add annotations as XML comments (only in Edit and Suggest modes, not Deployment)
                if (currentMode != EditMode.Deployment)
                {
                    AddAnnotationsAsComments(dataElement, key, language, username, currentMode == EditMode.Edit);
                }

                root.Add(dataElement);
            }
            
            // Create new document (no template to preserve)
            doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                root
            );
        }

        // Save and preserve trailing newlines from original file
        var savedContent = doc.ToString();
        
        // Check if original file exists and has trailing newlines
        if (File.Exists(filePath))
        {
            var originalContent = File.ReadAllText(filePath);
            var trailingNewlines = originalContent.Length - originalContent.TrimEnd('\r', '\n').Length;
            if (trailingNewlines > 0)
            {
                savedContent += originalContent.Substring(originalContent.Length - trailingNewlines);
            }
        }
        
        File.WriteAllText(filePath, savedContent);
    }

    /// <summary>
    /// Add suggestion annotations as XML comments after the value element
    /// Format: <!-- iso-lingo-audit:SUGGESTION:...,by:[username],at:[datetime] -->
    /// </summary>
    private void AddAnnotationsAsComments(XElement dataElement, TranslationKey key, string language, string? username, bool isEditMode)
    {
        // Add suggestion comment if exists for this language
        if (key.SuggestedValues.TryGetValue(language, out var suggestion))
        {
            dataElement.Add(new XText("    "));
            dataElement.Add(new XComment($" {suggestion.ToFileFormat()} "));
            // Add final newline and indentation before closing tag
            dataElement.Add(new XText("\n  "));
        }
    }

    private XElement CreateResxDocument()
    {
        var root = new XElement("root",
            // Schema definition
            CreateSchemaElement(),

            // Required resource headers
            new XElement("resheader",
                new XAttribute("name", "resmimetype"),
                new XElement("value", "text/microsoft-resx")
            ),
            new XElement("resheader",
                new XAttribute("name", "version"),
                new XElement("value", "2.0")
            ),
            new XElement("resheader",
                new XAttribute("name", "reader"),
                new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
            ),
            new XElement("resheader",
                new XAttribute("name", "writer"),
                new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
            )
        );

        return root;
    }

    private XElement CreateSchemaElement()
    {
        return new XElement(Xsd + "schema",
            new XAttribute("id", "root"),
            new XAttribute("xmlns", ""),
            new XAttribute(XNamespace.Xmlns + "xsd", Xsd),
            new XAttribute(XNamespace.Xmlns + "msdata", Msdata),
            new XElement(Xsd + "import",
                new XAttribute("namespace", "http://www.w3.org/XML/1998/namespace")
            ),
            new XElement(Xsd + "element",
                new XAttribute("name", "root"),
                new XAttribute(Msdata + "IsDataSet", "true"),
                CreateComplexTypeElement()
            )
        );
    }

    private XElement CreateComplexTypeElement()
    {
        return new XElement(Xsd + "complexType",
            new XElement(Xsd + "choice",
                new XAttribute("maxOccurs", "unbounded"),
                // metadata element
                new XElement(Xsd + "element",
                    new XAttribute("name", "metadata"),
                    new XElement(Xsd + "complexType",
                        new XElement(Xsd + "sequence",
                            new XElement(Xsd + "element",
                                new XAttribute("name", "value"),
                                new XAttribute("type", "xsd:string"),
                                new XAttribute("minOccurs", "0")
                            )
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "name"),
                            new XAttribute("use", "required"),
                            new XAttribute("type", "xsd:string")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "type"),
                            new XAttribute("type", "xsd:string")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "mimetype"),
                            new XAttribute("type", "xsd:string")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("ref", "xml:space")
                        )
                    )
                ),
                // assembly element
                new XElement(Xsd + "element",
                    new XAttribute("name", "assembly"),
                    new XElement(Xsd + "complexType",
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "alias"),
                            new XAttribute("type", "xsd:string")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "name"),
                            new XAttribute("type", "xsd:string")
                        )
                    )
                ),
                // data element
                new XElement(Xsd + "element",
                    new XAttribute("name", "data"),
                    new XElement(Xsd + "complexType",
                        new XElement(Xsd + "sequence",
                            new XElement(Xsd + "element",
                                new XAttribute("name", "value"),
                                new XAttribute("type", "xsd:string"),
                                new XAttribute("minOccurs", "0"),
                                new XAttribute(Msdata + "Ordinal", "1")
                            ),
                            new XElement(Xsd + "element",
                                new XAttribute("name", "comment"),
                                new XAttribute("type", "xsd:string"),
                                new XAttribute("minOccurs", "0"),
                                new XAttribute(Msdata + "Ordinal", "2")
                            )
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "name"),
                            new XAttribute("type", "xsd:string"),
                            new XAttribute("use", "required"),
                            new XAttribute(Msdata + "Ordinal", "1")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "type"),
                            new XAttribute("type", "xsd:string"),
                            new XAttribute(Msdata + "Ordinal", "3")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "mimetype"),
                            new XAttribute("type", "xsd:string"),
                            new XAttribute(Msdata + "Ordinal", "4")
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("ref", "xml:space")
                        )
                    )
                ),
                // resheader element
                new XElement(Xsd + "element",
                    new XAttribute("name", "resheader"),
                    new XElement(Xsd + "complexType",
                        new XElement(Xsd + "sequence",
                            new XElement(Xsd + "element",
                                new XAttribute("name", "value"),
                                new XAttribute("type", "xsd:string"),
                                new XAttribute("minOccurs", "0"),
                                new XAttribute(Msdata + "Ordinal", "1")
                            )
                        ),
                        new XElement(Xsd + "attribute",
                            new XAttribute("name", "name"),
                            new XAttribute("type", "xsd:string"),
                            new XAttribute("use", "required")
                        )
                    )
                )
            )
        );
    }
}
