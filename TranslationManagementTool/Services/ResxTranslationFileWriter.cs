using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TranslationManagementTool.Models;

namespace TranslationManagementTool.Services;

public class ResxTranslationFileWriter
{
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace Msdata = "urn:schemas-microsoft-com:xml-msdata";

    /// <summary>
    /// Writes translation keys to RESX files, grouped by language and source file
    /// </summary>
    public void WriteFiles(List<TranslationKey> keys, string outputDirectory)
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
                var fileName = $"{sourceFile}_{language}.resx";
                var filePath = Path.Combine(outputDirectory, fileName);

                WriteLanguageFile(filePath, fileKeys, language);
            }
        }
    }

    private void WriteLanguageFile(string filePath, List<TranslationKey> keys, string language)
    {
        var root = CreateResxDocument();

        // Add data elements for each key
        foreach (var key in keys.OrderBy(k => k.Key))
        {
            if (key.LanguageValues.TryGetValue(language, out var value))
            {
                var dataElement = new XElement("data",
                    new XAttribute("name", key.Key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value)
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
