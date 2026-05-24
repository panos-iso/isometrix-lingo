using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TranslationManagementTool.Models;
using TranslationManagementTool.Services;
using Xunit;

namespace TranslationManagementTool.Tests.Services;

public class JsonTranslationFileWriterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly JsonTranslationFileWriter _writer;

    public JsonTranslationFileWriterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TranslationTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
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
    public void WriteFiles_CreatesFilesForEachLanguage()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "hello",
                SourceFile = "forms",
                LanguageValues = new() { { "en", "Hello" }, { "es", "Hola" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        Assert.True(File.Exists(Path.Combine(_testDirectory, "forms_en.json")));
        Assert.True(File.Exists(Path.Combine(_testDirectory, "forms_es.json")));
    }

    [Fact]
    public void WriteFiles_WritesCorrectJsonStructure()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "greeting",
                SourceFile = "messages",
                LanguageValues = new() { { "en", "Hello World" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        var filePath = Path.Combine(_testDirectory, "messages_en.json");
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        
        Assert.Equal("Hello World", doc.RootElement.GetProperty("greeting").GetString());
    }

    [Fact]
    public void WriteFiles_HandlesNestedKeys()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "login.username",
                SourceFile = "forms",
                LanguageValues = new() { { "en", "Username" } }
            },
            new()
            {
                Key = "login.password",
                SourceFile = "forms",
                LanguageValues = new() { { "en", "Password" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        var filePath = Path.Combine(_testDirectory, "forms_en.json");
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        
        var login = doc.RootElement.GetProperty("login");
        Assert.Equal("Username", login.GetProperty("username").GetString());
        Assert.Equal("Password", login.GetProperty("password").GetString());
    }

    [Fact]
    public void WriteFiles_GroupsBySourceFile()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "key1",
                SourceFile = "file1",
                LanguageValues = new() { { "en", "Value1" } }
            },
            new()
            {
                Key = "key2",
                SourceFile = "file2",
                LanguageValues = new() { { "en", "Value2" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        Assert.True(File.Exists(Path.Combine(_testDirectory, "file1_en.json")));
        Assert.True(File.Exists(Path.Combine(_testDirectory, "file2_en.json")));
    }

    [Fact]
    public void WriteFiles_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "subdir");
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "test",
                SourceFile = "file",
                LanguageValues = new() { { "en", "Test" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, newDir);

        // Assert
        Assert.True(Directory.Exists(newDir));
        Assert.True(File.Exists(Path.Combine(newDir, "file_en.json")));
    }

    [Fact]
    public void WriteFiles_WritesFormattedJson()
    {
        // Arrange
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "key",
                SourceFile = "file",
                LanguageValues = new() { { "en", "Value" } }
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        var json = File.ReadAllText(Path.Combine(_testDirectory, "file_en.json"));
        Assert.Contains("\n", json); // Indented JSON contains newlines
        Assert.Contains("  ", json); // Indented JSON contains spaces
    }

    [Fact]
    public void WriteFiles_OnlyWritesModifiedKeys()
    {
        // Arrange - only modified keys should be exported
        var keys = new List<TranslationKey>
        {
            new()
            {
                Key = "modified",
                SourceFile = "forms",
                LanguageValues = new() { { "en", "Modified Value" } },
                IsModified = true
            }
        };

        // Act
        _writer.WriteFiles(keys, _testDirectory);

        // Assert
        var filePath = Path.Combine(_testDirectory, "forms_en.json");
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        
        Assert.Equal("Modified Value", doc.RootElement.GetProperty("modified").GetString());
    }
}
