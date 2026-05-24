using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IsometrixLingo.Models;
using IsometrixLingo.Services;
using Xunit;

namespace IsometrixLingo.Tests.Services;

public class ResxTemplatePreservationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ResxTranslationFileReader _reader;
    private readonly ResxTranslationFileWriter _writer;

    public ResxTemplatePreservationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ResxTemplateTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _reader = new ResxTranslationFileReader();
        _writer = new ResxTranslationFileWriter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ExtractTemplate_PreservesCompleteStructure()
    {
        // Arrange - use the sample RESX file
        var testDir = Directory.GetCurrentDirectory();
        var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        var samplePath = Path.Combine(solutionDir, "sample-translations", "FormTranslations.resx");

        // Act
        var template = _reader.ExtractTemplate(samplePath);

        // Assert
        var resHeaders = template.Descendants("resheader").ToList();
        Assert.NotEmpty(resHeaders);
        Assert.Contains(resHeaders, rh => rh.Attribute("name")?.Value == "resmimetype");
        Assert.Contains(resHeaders, rh => rh.Attribute("name")?.Value == "version");

        // Template should preserve data elements for updating during export
        var dataElements = template.Descendants("data").ToList();
        Assert.NotEmpty(dataElements);
    }

    [Fact]
    public void WriteFiles_WithTemplate_UpdatesExistingAndAddsNew()
    {
        // Arrange - Create a template with existing data
        var template = new XDocument(
            new XElement("root",
                new XElement("resheader",
                    new XAttribute("name", "resmimetype"),
                    new XElement("value", "text/microsoft-resx")
                ),
                new XElement("data",
                    new XAttribute("name", "ExistingKey"),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", "Old Value")
                ),
                new XElement("data",
                    new XAttribute("name", "AnotherKey"),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", "Another Old Value")
                )
            )
        );

        var keys = new List<TranslationKey>
        {
            // Update existing key
            new()
            {
                Key = "ExistingKey",
                Source = new SourceFile("test", FileType.Resx),
                LanguageValues = new() { { "en", "Updated Value" } }
            },
            // Keep another existing key unchanged
            new()
            {
                Key = "AnotherKey",
                Source = new SourceFile("test", FileType.Resx),
                LanguageValues = new() { { "en", "Another Old Value" } }
            },
            // Add new key
            new()
            {
                Key = "NewKey",
                Source = new SourceFile("test", FileType.Resx),
                LanguageValues = new() { { "en", "New Value" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory, _ => template);

        // Assert
        var outputFile = Path.Combine(_testDirectory, "test.resx");
        Assert.True(File.Exists(outputFile));

        var outputDoc = XDocument.Load(outputFile);
        var dataElements = outputDoc.Descendants("data").ToList();

        // Should have 3 data elements total
        Assert.Equal(3, dataElements.Count);

        // Existing key should be updated
        var existingData = dataElements.FirstOrDefault(d => d.Attribute("name")?.Value == "ExistingKey");
        Assert.NotNull(existingData);
        Assert.Equal("Updated Value", existingData.Element("value")?.Value);

        // Another key should remain unchanged
        var anotherData = dataElements.FirstOrDefault(d => d.Attribute("name")?.Value == "AnotherKey");
        Assert.NotNull(anotherData);
        Assert.Equal("Another Old Value", anotherData.Element("value")?.Value);

        // New key should be added
        var newData = dataElements.FirstOrDefault(d => d.Attribute("name")?.Value == "NewKey");
        Assert.NotNull(newData);
        Assert.Equal("New Value", newData.Element("value")?.Value);
    }

    [Fact]
    public void WriteFiles_WithoutTemplate_CreatesDefaultStructure()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "TestKey",
                Source = new SourceFile("test", FileType.Resx),
                LanguageValues = new() { { "en", "Test Value" } }
            }
        };

        // Act - no template provider
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        var outputFile = Path.Combine(_testDirectory, "test.resx");
        Assert.True(File.Exists(outputFile));

        var outputDoc = XDocument.Load(outputFile);

        // Should have standard resheaders
        var resHeaders = outputDoc.Descendants("resheader").ToList();
        Assert.NotEmpty(resHeaders);

        // Should have the data element
        var dataElement = outputDoc.Descendants("data")
            .FirstOrDefault(d => d.Attribute("name")?.Value == "TestKey");
        Assert.NotNull(dataElement);
    }
}
