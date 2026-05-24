using System;
using System.IO;
using System.Linq;
using TranslationManagementTool.Models;
using TranslationManagementTool.Services;
using Xunit;

namespace TranslationManagementTool.Tests.Services;

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
    public void ExtractLanguage_ShouldReturnCorrectLanguageCode(string fileName, string expectedLanguage)
    {
        var result = _reader.ExtractLanguage(fileName);
        Assert.Equal(expectedLanguage, result);
    }

    [Theory]
    [InlineData("forms_en.resx", "forms")]
    [InlineData("errors_es.resx", "errors")]
    [InlineData("MyApp.Strings_fr.resx", "MyApp.Strings")]
    public void ExtractBaseFileName_ShouldReturnFileNameWithoutLanguage(string fileName, string expected)
    {
        var result = _reader.ExtractBaseFileName(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractLanguage_ShouldThrow_WhenLanguageCodeMissing()
    {
        Assert.Throws<ArgumentException>(() => _reader.ExtractLanguage("forms.resx"));
    }

    [Fact]
    public void ReadFile_ShouldParseResxFile()
    {
        // Arrange
        var filePath = Path.Combine(_samplePath, "forms_en.resx");

        // Act
        var result = _reader.ReadFile(filePath);

        // Assert
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal("en", result.Language);
        Assert.Equal(FileType.Resx, result.FileType);
        Assert.Equal(4, result.Keys.Count);
    }

    [Fact]
    public void ReadFile_ShouldParseAllDataElements()
    {
        // Arrange
        var filePath = Path.Combine(_samplePath, "forms_en.resx");

        // Act
        var result = _reader.ReadFile(filePath);
        var keys = result.Keys.OrderBy(k => k.Key).ToList();

        // Assert
        Assert.Equal("button.cancel", keys[0].Key);
        Assert.Equal("Cancel", keys[0].LanguageValues["en"]);

        Assert.Equal("button.save", keys[1].Key);
        Assert.Equal("Save", keys[1].LanguageValues["en"]);

        Assert.Equal("label.password", keys[2].Key);
        Assert.Equal("Password", keys[2].LanguageValues["en"]);

        Assert.Equal("label.username", keys[3].Key);
        Assert.Equal("Username", keys[3].LanguageValues["en"]);
    }

    [Fact]
    public void ConsolidateKeys_ShouldMergeKeysFromMultipleFiles()
    {
        // Arrange
        var enFile = _reader.ReadFile(Path.Combine(_samplePath, "forms_en.resx"));
        var esFile = _reader.ReadFile(Path.Combine(_samplePath, "forms_es.resx"));

        // Act
        var result = _reader.ConsolidateKeys(new() { enFile, esFile });

        // Assert
        Assert.Equal(4, result.Count); // 4 unique keys total

        var cancelButton = result.First(k => k.Key == "button.cancel");
        Assert.Equal(2, cancelButton.LanguageValues.Count);
        Assert.Equal("Cancel", cancelButton.LanguageValues["en"]);
        Assert.Equal("Cancelar", cancelButton.LanguageValues["es"]);
    }

    [Fact]
    public void ConsolidateKeys_ShouldHandleMissingKeys()
    {
        // Arrange
        var enFile = _reader.ReadFile(Path.Combine(_samplePath, "forms_en.resx"));
        var esFile = _reader.ReadFile(Path.Combine(_samplePath, "forms_es.resx"));

        // Act
        var result = _reader.ConsolidateKeys(new() { enFile, esFile });

        // Assert - English has "label.password" but Spanish doesn't
        var passwordLabel = result.First(k => k.Key == "label.password");
        Assert.Single(passwordLabel.LanguageValues); // Only English
        Assert.Equal("Password", passwordLabel.LanguageValues["en"]);
        Assert.False(passwordLabel.LanguageValues.ContainsKey("es"));
    }

    [Fact]
    public void ConsolidateKeys_ShouldSortKeysByName()
    {
        // Arrange
        var enFile = _reader.ReadFile(Path.Combine(_samplePath, "forms_en.resx"));

        // Act
        var result = _reader.ConsolidateKeys(new() { enFile });

        // Assert
        Assert.Equal("button.cancel", result[0].Key);
        Assert.Equal("button.save", result[1].Key);
        Assert.Equal("label.password", result[2].Key);
        Assert.Equal("label.username", result[3].Key);
    }
}
