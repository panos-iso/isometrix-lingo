using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IsometrixLingo.Models;
using IsometrixLingo.Services;
using Xunit;

namespace IsometrixLingo.Tests.Services;

public class JsonTemplatePreservationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly JsonTranslationFileReader _reader;
    private readonly JsonTranslationFileWriter _writer;

    public JsonTemplatePreservationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"JsonTemplateTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _reader = new JsonTranslationFileReader();
        _writer = new JsonTranslationFileWriter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ExtractTemplate_PreservesOriginalJson()
    {
        // Arrange
        var jsonContent = @"{
  ""key1"": ""value1"",
  ""nested"": {
    ""key2"": ""value2""
  }
}";
        var filePath = Path.Combine(_testDirectory, "test.en.json");
        File.WriteAllText(filePath, jsonContent);

        // Act
        var template = _reader.ExtractTemplate(filePath);

        // Assert
        Assert.NotNull(template);
        Assert.Contains("key1", template);
        Assert.Contains("nested", template);
    }

    [Fact]
    public void WriteFiles_WithTemplate_UpdatesExistingAndAddsNew()
    {
        // Arrange - Create a template with existing structure
        var template = @"{
  ""ExistingKey"": ""Old Value"",
  ""nested"": {
    ""AnotherKey"": ""Another Old Value""
  }
}";

        var keys = new List<TranslationKey>
        {
            // Update existing flat key
            new()
            {
                Key = "ExistingKey",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "Updated Value" } }
            },
            // Update existing nested key
            new()
            {
                Key = "nested.AnotherKey",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "Updated Nested Value" } }
            },
            // Add new key
            new()
            {
                Key = "NewKey",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "New Value" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory, _ => template);

        // Assert
        var outputFile = Path.Combine(_testDirectory, "test.en.json");
        Assert.True(File.Exists(outputFile));

        var outputJson = File.ReadAllText(outputFile);
        var doc = JsonDocument.Parse(outputJson);

        // Existing key should be updated
        Assert.Equal("Updated Value", doc.RootElement.GetProperty("ExistingKey").GetString());

        // Nested key should be updated
        Assert.Equal("Updated Nested Value", doc.RootElement.GetProperty("nested").GetProperty("AnotherKey").GetString());

        // New key should be added
        Assert.Equal("New Value", doc.RootElement.GetProperty("NewKey").GetString());
    }

    [Fact]
    public void WriteFiles_WithoutTemplate_CreatesFromScratch()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "TestKey",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "Test Value" } }
            },
            new()
            {
                Key = "nested.NestedKey",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "Nested Value" } }
            }
        };

        // Act - no template provider
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        var outputFile = Path.Combine(_testDirectory, "test.en.json");
        Assert.True(File.Exists(outputFile));

        var outputJson = File.ReadAllText(outputFile);
        var doc = JsonDocument.Parse(outputJson);

        Assert.Equal("Test Value", doc.RootElement.GetProperty("TestKey").GetString());
        Assert.Equal("Nested Value", doc.RootElement.GetProperty("nested").GetProperty("NestedKey").GetString());
    }

    [Fact]
    public void WriteFiles_PreservesKeyOrder()
    {
        // Arrange - Template with specific key order
        var template = @"{
  ""zebra"": ""last"",
  ""apple"": ""first"",
  ""middle"": ""center""
}";

        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "zebra",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "updated last" } }
            },
            new()
            {
                Key = "apple",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "updated first" } }
            },
            new()
            {
                Key = "middle",
                Source = new SourceFile("test", FileType.Json),
                LanguageValues = new() { { "en", "updated center" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory, _ => template);

        // Assert
        var outputFile = Path.Combine(_testDirectory, "test.en.json");
        var outputJson = File.ReadAllText(outputFile);

        // Verify order is preserved (zebra should appear before apple in the JSON text)
        var zebraIndex = outputJson.IndexOf("\"zebra\"");
        var appleIndex = outputJson.IndexOf("\"apple\"");
        var middleIndex = outputJson.IndexOf("\"middle\"");

        Assert.True(zebraIndex < appleIndex, "zebra should appear before apple");
        Assert.True(appleIndex < middleIndex, "apple should appear before middle");
    }
}
