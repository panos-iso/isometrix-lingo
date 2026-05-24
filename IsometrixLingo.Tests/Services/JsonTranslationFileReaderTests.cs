using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IsometrixLingo.Models;
using IsometrixLingo.Services;
using Xunit;

namespace IsometrixLingo.Tests.Services;

public class JsonTranslationFileReaderTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly JsonTranslationFileReader _reader;

    public JsonTranslationFileReaderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"json_reader_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _reader = new JsonTranslationFileReader();
    }

    [Theory]
    [InlineData("forms_en.json", "en")]
    [InlineData("forms_es.json", "es")]
    [InlineData("app.fr.json", "fr")]
    [InlineData("Resources_de.json", "de")]
    public void ExtractLanguage_ReturnsCorrectLanguage(string fileName, string expectedLanguage)
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, fileName);

        // Act
        var language = _reader.ExtractLanguage(filePath);

        // Assert
        Assert.Equal(expectedLanguage, language);
    }

    [Theory]
    [InlineData("forms_en.json", "forms")]
    [InlineData("forms_es.json", "forms")]
    [InlineData("app.fr.json", "app")]
    [InlineData("Resources_de.json", "Resources")]
    public void ExtractBaseFileName_ReturnsCorrectBaseName(string fileName, string expectedBaseName)
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, fileName);

        // Act
        var baseName = _reader.ExtractBaseFileName(filePath);

        // Assert
        Assert.Equal(expectedBaseName, baseName);
    }

    [Fact]
    public void ReadFile_ParsesSimpleJsonCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test_en.json");
        var jsonContent = @"{
            ""hello"": ""Hello"",
            ""goodbye"": ""Goodbye""
        }";
        File.WriteAllText(filePath, jsonContent);

        // Act
        var translationFile = _reader.ReadFile(filePath);

        // Assert
        Assert.Equal("en", translationFile.Language);
        Assert.Equal("test", _reader.ExtractBaseFileName(filePath));
        Assert.Equal(FileType.Json, translationFile.FileType);
        Assert.Equal(2, translationFile.Keys.Count);
        Assert.Contains(translationFile.Keys, k => k.Key == "hello");
        Assert.Contains(translationFile.Keys, k => k.Key == "goodbye");
    }

    [Fact]
    public void ReadFile_ParsesNestedJsonCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nested_en.json");
        var jsonContent = @"{
            ""common"": {
                ""hello"": ""Hello"",
                ""goodbye"": ""Goodbye""
            },
            ""errors"": {
                ""notFound"": ""Not found""
            }
        }";
        File.WriteAllText(filePath, jsonContent);

        // Act
        var translationFile = _reader.ReadFile(filePath);

        // Assert
        Assert.Equal(3, translationFile.Keys.Count);
        Assert.Contains(translationFile.Keys, k => k.Key == "common.hello");
        Assert.Contains(translationFile.Keys, k => k.Key == "common.goodbye");
        Assert.Contains(translationFile.Keys, k => k.Key == "errors.notFound");
    }

    [Fact]
    public void ConsolidateKeys_MergesKeysFromMultipleFiles()
    {
        // Arrange
        var enFilePath = Path.Combine(_testDirectory, "forms_en.json");
        var esFilePath = Path.Combine(_testDirectory, "forms_es.json");

        File.WriteAllText(enFilePath, @"{""hello"": ""Hello"", ""goodbye"": ""Goodbye""}");
        File.WriteAllText(esFilePath, @"{""hello"": ""Hola""}");

        var enFile = _reader.ReadFile(enFilePath);
        var esFile = _reader.ReadFile(esFilePath);

        var files = new List<TranslationFile> { enFile, esFile };

        // Act
        var consolidated = _reader.ConsolidateKeys(files);

        // Assert
        Assert.Equal(2, consolidated.Count);

        var helloKey = consolidated.First(k => k.Key == "hello");
        Assert.Equal("Hello", helloKey.LanguageValues["en"]);
        Assert.Equal("Hola", helloKey.LanguageValues["es"]);

        var goodbyeKey = consolidated.First(k => k.Key == "goodbye");
        Assert.Equal("Goodbye", goodbyeKey.LanguageValues["en"]);
        Assert.Equal(string.Empty, goodbyeKey.LanguageValues["es"]); // Missing key shows as empty
    }

    [Fact]
    public void ConsolidateKeys_FillsMissingLanguageValuesWithEmpty()
    {
        // Arrange
        var enFilePath = Path.Combine(_testDirectory, "app_en.json");
        var esFilePath = Path.Combine(_testDirectory, "app_es.json");
        var frFilePath = Path.Combine(_testDirectory, "app_fr.json");

        File.WriteAllText(enFilePath, @"{""key1"": ""Value1"", ""key2"": ""Value2""}");
        File.WriteAllText(esFilePath, @"{""key1"": ""Valor1""}");
        File.WriteAllText(frFilePath, @"{""key2"": ""Valeur2""}");

        var enFile = _reader.ReadFile(enFilePath);
        var esFile = _reader.ReadFile(esFilePath);
        var frFile = _reader.ReadFile(frFilePath);

        var files = new List<TranslationFile> { enFile, esFile, frFile };

        // Act
        var consolidated = _reader.ConsolidateKeys(files);

        // Assert
        var key1 = consolidated.First(k => k.Key == "key1");
        Assert.Equal("Value1", key1.LanguageValues["en"]);
        Assert.Equal("Valor1", key1.LanguageValues["es"]);
        Assert.Equal(string.Empty, key1.LanguageValues["fr"]);

        var key2 = consolidated.First(k => k.Key == "key2");
        Assert.Equal("Value2", key2.LanguageValues["en"]);
        Assert.Equal(string.Empty, key2.LanguageValues["es"]);
        Assert.Equal("Valeur2", key2.LanguageValues["fr"]);
    }

    [Fact]
    public void ReadFile_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _reader.ReadFile(nonExistentPath));
    }

    [Fact]
    public void ReadFile_ThrowsInvalidOperationException_WhenJsonIsInvalid()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "invalid_en.json");
        File.WriteAllText(filePath, "{ invalid json }");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _reader.ReadFile(filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
