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
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Value1" } } },
            new() { Key = "key2", Source = new SourceFile("file2", FileType.Json), LanguageValues = new() { { "en", "Value2" } } }
        };

        // Act
        store.AddTranslations(keys);

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
        Assert.Equal(2, store.SourceFiles.Count);
        Assert.Contains(store.SourceFiles, sf => sf.Name == "file1" && sf.Type == FileType.Json);
        Assert.Contains(store.SourceFiles, sf => sf.Name == "file2" && sf.Type == FileType.Json);
    }

    [Fact]
    public void FilterBySourceFiles_WithNoFilter_ShowsAllKeys()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() },
            new() { Key = "key2", Source = new SourceFile("file2", FileType.Json), LanguageValues = new() }
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
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() },
            new() { Key = "key2", Source = new SourceFile("file2", FileType.Json), LanguageValues = new() },
            new() { Key = "key3", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() }
        };
        store.AddTranslations(keys);

        // Act
        store.FilterBySourceFiles(new List<SourceFile> { new SourceFile("file1", FileType.Json) });

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
        Assert.All(store.FilteredKeys, k => Assert.Equal("file1", k.Source.Name));
    }

    [Fact]
    public void Search_FindsMatchingKeyNames()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "hello", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Hello" } } },
            new() { Key = "goodbye", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Goodbye" } } }
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
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Hello" }, { "es", "Hola" } } },
            new() { Key = "key2", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Goodbye" } } }
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
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Original" } } }
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
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() }
        };
        store.AddTranslations(keys);

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.FilteredKeys);
        Assert.Empty(store.SourceFiles);
    }

    [Fact]
    public void AddTranslations_TracksLanguages()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Hello" }, { "es", "Hola" } } },
            new() { Key = "key2", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Goodbye" }, { "fr", "Au revoir" } } }
        };

        // Act
        store.AddTranslations(keys);

        // Assert - Only supported languages (en and es) are tracked
        Assert.Equal(2, store.Languages.Count);
        Assert.Contains("en", store.Languages);
        Assert.Contains("es", store.Languages);

        // Verify that unsupported language (fr) was filtered out
        var key2 = store.FilteredKeys.First(k => k.Key == "key2");
        Assert.DoesNotContain("fr", key2.LanguageValues.Keys);
    }

    [Fact]
    public void Clear_RemovesKeysAndSourceFiles()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", Source = new SourceFile("file1", FileType.Json), LanguageValues = new() { { "en", "Hello" } } }
        };
        store.AddTranslations(keys);

        // Act
        store.Clear();

        // Assert - Languages remain static (en and es)
        Assert.Equal(2, store.Languages.Count);
        Assert.Contains("en", store.Languages);
        Assert.Contains("es", store.Languages);

        // But keys and source files are cleared
        Assert.Empty(store.FilteredKeys);
        Assert.Empty(store.SourceFiles);
    }

    [Fact]
    public void FilterBySearchTerm_FiltersKeysByKeyName()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "login.username", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Username" } } },
            new() { Key = "login.password", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Password" } } },
            new() { Key = "menu.home", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Home" } } }
        };
        store.AddTranslations(keys);

        // Act
        store.FilterBySearchTerm("login");

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
        Assert.All(store.FilteredKeys, k => Assert.Contains("login", k.Key));
    }

    [Fact]
    public void FilterBySearchTerm_FiltersKeysByLanguageValue()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Hello World" }, { "es", "Hola Mundo" } } },
            new() { Key = "key2", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Goodbye" }, { "es", "Adiós" } } },
            new() { Key = "key3", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Welcome" }, { "es", "Bienvenido" } } }
        };
        store.AddTranslations(keys);

        // Act
        store.FilterBySearchTerm("mundo");

        // Assert
        Assert.Single(store.FilteredKeys);
        Assert.Equal("key1", store.FilteredKeys[0].Key);
    }

    [Fact]
    public void FilterBySearchTerm_CombinedWithFileFilter()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "login.username", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Username" } } },
            new() { Key = "login.password", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Password" } } },
            new() { Key = "menu.login", Source = new SourceFile("menu", FileType.Json), LanguageValues = new() { { "en", "Login" } } }
        };
        store.AddTranslations(keys);

        // Act - filter by file first
        store.FilterBySourceFiles(new List<SourceFile> { new SourceFile("forms", FileType.Json) });
        // Then search
        store.FilterBySearchTerm("login");

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
        Assert.All(store.FilteredKeys, k => Assert.Equal("forms", k.Source.Name));
        Assert.All(store.FilteredKeys, k => Assert.Contains("login", k.Key));
    }

    [Fact]
    public void FilterBySearchTerm_EmptySearchShowsAll()
    {
        // Arrange
        var store = new TranslationStore();
        var keys = new List<TranslationKey>
        {
            new() { Key = "key1", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Value1" } } },
            new() { Key = "key2", Source = new SourceFile("forms", FileType.Json), LanguageValues = new() { { "en", "Value2" } } }
        };
        store.AddTranslations(keys);
        store.FilterBySearchTerm("key1"); // Filter first

        // Act - clear search
        store.FilterBySearchTerm("");

        // Assert
        Assert.Equal(2, store.FilteredKeys.Count);
    }
}
