using System.Collections.Generic;
using System.Linq;
using TranslationManagementTool.Models;
using TranslationManagementTool.Services;
using Xunit;

namespace TranslationManagementTool.Tests.Services;

public class TranslationStoreTests
{
    [Fact]
    public void AddTranslations_AddsKeysAndSourceFiles()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", SourceFile = "file1", LanguageValues = new() { { "en", "Value1" } } },
            new() { Key = "key2", SourceFile = "file2", LanguageValues = new() { { "en", "Value2" } } }
        };

        // Act
        store.AddTranslations(keys);

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
        Assert.Equal(2, store.SourceFiles.Count);
        Assert.Contains("file1", store.SourceFiles);
        Assert.Contains("file2", store.SourceFiles);
    }

    [Fact]
    public void FilterBySourceFiles_WithNoFilter_ShowsAllKeys()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", SourceFile = "file1", LanguageValues = new() },
            new() { Key = "key2", SourceFile = "file2", LanguageValues = new() }
        };
        store.AddTranslations(keys);

        // Act
        store.FilterBySourceFiles(null!);

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
    }

    [Fact]
    public void FilterBySourceFiles_WithFilter_ShowsOnlyMatchingKeys()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", SourceFile = "file1", LanguageValues = new() },
            new() { Key = "key2", SourceFile = "file2", LanguageValues = new() },
            new() { Key = "key3", SourceFile = "file1", LanguageValues = new() }
        };
        store.AddTranslations(keys);

        // Act
        store.FilterBySourceFiles(new List<string> { "file1" });

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
        Assert.All(store.FilteredKeys, k => Assert.Equal("file1", k.SourceFile));
    }

    [Fact]
    public void Search_FindsMatchingKeyNames()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "hello", SourceFile = "file1", LanguageValues = new() { { "en", "Hello" } } },
            new() { Key = "goodbye", SourceFile = "file1", LanguageValues = new() { { "en", "Goodbye" } } }
        };
        store.AddTranslations(keys);

        // Act
        var results = store.Search("hello");

        // Assert
        Assert.Single(results);
        Assert.Equal("hello", results[0].Key);
    }

    [Fact]
    public void Search_FindsMatchingLanguageValues()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", SourceFile = "file1", LanguageValues = new() { { "en", "Hello" }, { "es", "Hola" } } },
            new() { Key = "key2", SourceFile = "file1", LanguageValues = new() { { "en", "Goodbye" } } }
        };
        store.AddTranslations(keys);

        // Act
        var results = store.Search("Hola");

        // Assert
        Assert.Single(results);
        Assert.Equal("key1", results[0].Key);
    }

    [Fact]
    public void UpdateTranslation_MarksKeyAsModified()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", SourceFile = "file1", LanguageValues = new() { { "en", "Original" } } }
        };
        store.AddTranslations(keys);

        // Act
        store.UpdateTranslation("key1", "en", "Updated");

        // Assert
        var modifiedKeys = store.GetModifiedKeys();
        Assert.Single(modifiedKeys);
        Assert.True(modifiedKeys[0].IsModified);
        Assert.Equal("Updated", modifiedKeys[0].LanguageValues["en"]);
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", SourceFile = "file1", LanguageValues = new() }
        };
        store.AddTranslations(keys);

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.FilteredKeys);
        Assert.Empty(store.SourceFiles);
    }
}
