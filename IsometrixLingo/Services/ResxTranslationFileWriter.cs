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
    /// Writes translation keys to RESX files, grouped by language and source file
    /// </summary>
    /// <param name="keys">Translation keys to write</param>
    /// <param name="outputDirectory">Output directory for files</param>
    /// <param name="templateProvider">Optional function to provide RESX template for a given source file name</param>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory, Func<string, XDocument?>? templateProvider = null)
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
                // For English, use base filename without language code; for others, add underscore + language
                var fileName = language == "en"
                    ? $"{sourceFile}.resx"
                    : $"{sourceFile}_{language}.resx";
                var filePath = Path.Combine(outputDirectory, fileName);

                // Get template for this source file if available
                var template = templateProvider?.Invoke(sourceFile);

                WriteLanguageFile(filePath, fileKeys, language, template);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language, XDocument? template)
    {
        XElement root;

        if (template != null)
        {
            // Use the provided template (clone it to avoid modifying the original)
            var clonedTemplate = new XDocument(template);
            root = clonedTemplate.Root ?? throw new InvalidOperationException("Template root is null");

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
                        // Get actual value and append suggestion if exists
                        var value = translationKey.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                        var fullValue = AppendSuggestionIfExists(value, translationKey, language);
                        // Update the value in the existing data element
                        var valueElement = dataElement.Element("value");
                        if (valueElement != null)
                        {
                            valueElement.Value = fullValue;
                        }
                        else
                        {
                            dataElement.Add(new XElement("value", fullValue));
                        }
                        processedKeys.Add(keyName);
                    }
                }
            }

            // Add new data elements for keys that weren't in the template
            foreach (var key in keys.OrderBy(k => k.Key))
            {
                if (!processedKeys.Contains(key.Key))
                {
                    var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                    var fullValue = AppendSuggestionIfExists(value, key, language);
                    var dataElement = new XElement("data",
                        new XAttribute("name", key.Key),
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        new XElement("value", fullValue)
                    );

                    root.Add(dataElement);
                }
            }
        }
        else
        {
            // Fall back to creating a new RESX structure
            root = CreateResxDocument();

            // Add data elements for each key
            foreach (var key in keys.OrderBy(k => k.Key))
            {
                var value = key.LanguageValues.TryGetValue(language, out var val) ? val : string.Empty;
                var fullValue = AppendSuggestionIfExists(value, key, language);
                var dataElement = new XElement("data",
                    new XAttribute("name", key.Key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", fullValue)
                );

                root.Add(dataElement);
            }
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            root
        );

        doc.Save(filePath);
    }

    /// <summary>
    /// Append suggestion to value if it exists for the given language
    /// Format: "actual value SUGGESTION:suggested_value,by:[username],at:[datetime]"
    /// </summary>
    private string AppendSuggestionIfExists(string actualValue, TranslationKey key, string language)
    {
        if (key.SuggestedValues.TryGetValue(language, out var suggestion))
        {
            return $"{actualValue} {suggestion.ToFileFormat()}";
        }
        return actualValue;
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
