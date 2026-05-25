using System;
using System.IO;
using System.Linq;
using IsometrixLingo.Models;
using IsometrixLingo.Services;
using Xunit;

namespace IsometrixLingo.Tests.Services;

public class ResxTranslationFileReaderTests
{
    private readonly ResxTranslationFileReader _reader = new();
    private readonly string _samplePath;

    public ResxTranslationFileReaderTests()
    {
        // Navigate up from test bin directory to solution root
        var testDir = Directory.GetCurrentDirectory();
        var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        _samplePath = Path.Combine(solutionDir, "sample-translations");
    }

    [Theory]
    [InlineData("forms_en.resx", "en")]
    [InlineData("forms_es.resx", "es")]
    [InlineData("errors_fr.resx", "fr")]
    [InlineData("MyApp.Strings_de.resx", "de")]
    [InlineData("forms.resx", "en")] // Base file without language code defaults to English
    public void ExtractLanguage_ShouldReturnCorrectLanguageCode(string fileName, string expectedLanguage)
    {
        var result = _reader.ExtractLanguage(fileName);
        Assert.Equal(expectedLanguage, result);
    }

    [Theory]
    [InlineData("forms_en.resx", "forms")]
    [InlineData("errors_es.resx", "errors")]
    [InlineData("MyApp.Strings_fr.resx", "MyApp.Strings")]
    [InlineData("forms.resx", "forms")] // Base file without language code
    public void ExtractBaseFileName_ShouldReturnFileNameWithoutLanguage(string fileName, string expected)
    {
        var result = _reader.ExtractBaseFileName(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractLanguage_ShouldDefaultToEnglish_WhenLanguageCodeMissing()
    {
        // RESX files without language code (e.g., "forms.resx") are treated as English
        var result = _reader.ExtractLanguage("forms.resx");
        Assert.Equal("en", result);
    }

    [Fact]
    public void ReadFile_ShouldParseResxFile()
    {
        // Arrange
        var filePath = Path.Combine(_samplePath, "FormTranslations.resx");

        // Act
        var result = _reader.ReadFile(filePath);

        // Assert
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal("en", result.Language);
        Assert.Equal(FileType.Resx, result.FileType);
        Assert.Equal(12, result.Keys.Count);
    }

    [Fact]
    public void ReadFile_ShouldParseAllDataElements()
    {
        // Arrange
        var filePath = Path.Combine(_samplePath, "FormTranslations.resx");

        // Act
        var result = _reader.ReadFile(filePath);
        var keys = result.Keys.OrderBy(k => k.Key).ToList();

        // Assert
        Assert.Equal("Answer", keys[0].Key);
        Assert.Equal("Answer", keys[0].LanguageValues["en"]);

        Assert.Equal("Cancel", keys[1].Key);
        Assert.Equal("Cancel", keys[1].LanguageValues["en"]);

        Assert.Equal("CompletedOn", keys[2].Key);
        Assert.Equal("Completed On", keys[2].LanguageValues["en"]);
    }

    [Fact]
    public void ConsolidateKeys_ShouldMergeKeysFromMultipleFiles()
    {
        // Arrange
        var enFile = _reader.ReadFile(Path.Combine(_samplePath, "FormTranslations.resx"));
        var esFile = _reader.ReadFile(Path.Combine(_samplePath, "FormTranslations_es.resx"));

        // Act
        var result = _reader.ConsolidateKeys(new() { enFile, esFile });

        // Assert
        Assert.Equal(12, result.Count); // 12 unique keys total (9 original + 3 edge cases)

        var cancelButton = result.First(k => k.Key == "Cancel");
        Assert.Equal(2, cancelButton.LanguageValues.Count);
        Assert.Equal("Cancel", cancelButton.LanguageValues["en"]);
        Assert.Equal("Cancelar", cancelButton.LanguageValues["es"]);
    }

    [Fact]
    public void ConsolidateKeys_ShouldHandleMissingKeys()
    {
        // Arrange
        var enFile = _reader.ReadFile(Path.Combine(_samplePath, "FormTranslations.resx"));
        var esFile = _reader.ReadFile(Path.Combine(_samplePath, "FormTranslations_es.resx"));

        // Act
        var result = _reader.ConsolidateKeys(new() { enFile, esFile });

        // Both files have the same keys, so check all have both languages
        foreach (var key in result)
        {
            Assert.Equal(2, key.LanguageValues.Count);
            Assert.True(key.LanguageValues.ContainsKey("en"));
            Assert.True(key.LanguageValues.ContainsKey("es"));
        }
    }

    [Fact]
    public void ConsolidateKeys_ShouldSortKeysByName()
    {
        // Arrange
        var enFile = _reader.ReadFile(Path.Combine(_samplePath, "FormTranslations.resx"));

        // Act
        var result = _reader.ConsolidateKeys(new() { enFile });

        // Assert - keys sorted alphabetically
        Assert.Equal("Answer", result[0].Key);
        Assert.Equal("Cancel", result[1].Key);
        Assert.Equal("CompletedOn", result[2].Key);
    }
}
