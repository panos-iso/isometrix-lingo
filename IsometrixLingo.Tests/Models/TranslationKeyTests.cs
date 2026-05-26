using System.Collections.Generic;
using IsometrixLingo.Models;
using Xunit;

namespace IsometrixLingo.Tests.Models;

public class TranslationKeyTests
{
    [Fact]
    public void AcceptSuggestionForLanguage_AppliesValueAndRemovesSuggestion()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "Original English" },
                { "es", "Original Spanish" }
            },
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                { "en", new Suggestion { Value = "Suggested English", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act
        var acceptedValue = key.AcceptSuggestionForLanguage("en");

        // Assert
        Assert.Equal("Suggested English", acceptedValue);
        Assert.Equal("Suggested English", key.LanguageValues["en"]);
        Assert.False(key.SuggestedValues.ContainsKey("en"));
        Assert.Contains("en", key.ModifiedLanguages);
        Assert.True(key.IsModified);
    }

    [Fact]
    public void AcceptSuggestionForLanguage_ReturnsNullIfNoSuggestion()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "Original English" }
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        var acceptedValue = key.AcceptSuggestionForLanguage("en");

        // Assert
        Assert.Null(acceptedValue);
        Assert.Equal("Original English", key.LanguageValues["en"]);
    }

    [Fact]
    public void RejectSuggestionForLanguage_RemovesSuggestion()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "Original English" }
            },
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                { "en", new Suggestion { Value = "Suggested English", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act
        var wasRejected = key.RejectSuggestionForLanguage("en");

        // Assert
        Assert.True(wasRejected);
        Assert.False(key.SuggestedValues.ContainsKey("en"));
        Assert.Equal("Original English", key.LanguageValues["en"]); // Value unchanged
    }

    [Fact]
    public void RejectSuggestionForLanguage_ReturnsFalseIfNoSuggestion()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        var wasRejected = key.RejectSuggestionForLanguage("en");

        // Assert
        Assert.False(wasRejected);
    }

    [Fact]
    public void SetSuggestionForLanguage_AddsOrUpdatesSuggestion()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "Original English" }
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        key.SetSuggestionForLanguage("en", "New Suggestion", "testuser");

        // Assert
        Assert.True(key.SuggestedValues.ContainsKey("en"));
        Assert.Equal("New Suggestion", key.SuggestedValues["en"].Value);
        Assert.Equal("testuser", key.SuggestedValues["en"].Username);
    }

    [Fact]
    public void SetSuggestionForLanguage_RemovesSuggestionWhenValueIsNull()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                { "en", new Suggestion { Value = "Old Suggestion", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act
        key.SetSuggestionForLanguage("en", null, "testuser");

        // Assert
        Assert.False(key.SuggestedValues.ContainsKey("en"));
    }

    [Fact]
    public void HasAnySuggestions_ReturnsTrueWhenSuggestionsExist()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                { "en", new Suggestion { Value = "Suggestion", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Assert
        Assert.True(key.HasAnySuggestions);
    }

    [Fact]
    public void HasAnySuggestions_ReturnsFalseWhenNoSuggestions()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Assert
        Assert.False(key.HasAnySuggestions);
    }

    [Fact]
    public void UpdateMissingTranslationsStatus_DetectsMissingEnglish()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "es", "Spanish value" }
                // Missing "en"
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        key.UpdateMissingTranslationsStatus();

        // Assert
        Assert.True(key.HasMissingTranslations);
    }

    [Fact]
    public void UpdateMissingTranslationsStatus_DetectsMissingSpanish()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" }
                // Missing "es"
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        key.UpdateMissingTranslationsStatus();

        // Assert
        Assert.True(key.HasMissingTranslations);
    }

    [Fact]
    public void UpdateMissingTranslationsStatus_AcceptsSuggestionAsValid()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" }
                // Missing actual Spanish value
            },
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                { "es", new Suggestion { Value = "Spanish suggestion", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act
        key.UpdateMissingTranslationsStatus();

        // Assert - Suggestion counts as valid, so not missing
        Assert.False(key.HasMissingTranslations);
    }

    [Fact]
    public void UpdateMissingTranslationsStatus_DetectsEmptyValues()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "" } // Empty Spanish
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        key.UpdateMissingTranslationsStatus();

        // Assert
        Assert.True(key.HasMissingTranslations);
    }

    [Fact]
    public void UpdateMissingTranslationsStatus_DetectsWhitespaceValues()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "   " } // Whitespace Spanish
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        key.UpdateMissingTranslationsStatus();

        // Assert
        Assert.True(key.HasMissingTranslations);
    }

    [Fact]
    public void UpdateMissingTranslationsStatus_AllValidWhenBothLanguagesPresent()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "Spanish value" }
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
        };

        // Act
        key.UpdateMissingTranslationsStatus();

        // Assert
        Assert.False(key.HasMissingTranslations);
    }

    [Fact]
    public void IsConfirmed_ReturnsTrueWhenBothLanguagesAndConfirmationExist()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "Spanish value" }
            },
            ConfirmedBy = new Confirmation
            {
                Username = "TestUser",
                Timestamp = System.DateTime.UtcNow
            }
        };

        // Assert
        Assert.True(key.IsConfirmed);
    }

    [Fact]
    public void IsConfirmed_ReturnsFalseWhenNoConfirmation()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "Spanish value" }
            },
            ConfirmedBy = null
        };

        // Assert
        Assert.False(key.IsConfirmed);
    }

    [Fact]
    public void IsConfirmed_ReturnsFalseWhenMissingEnglish()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "" },
                { "es", "Spanish value" }
            },
            ConfirmedBy = new Confirmation
            {
                Username = "TestUser",
                Timestamp = System.DateTime.UtcNow
            }
        };

        // Assert
        Assert.False(key.IsConfirmed);
    }

    [Fact]
    public void IsConfirmed_ReturnsFalseWhenMissingSpanish()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "" }
            },
            ConfirmedBy = new Confirmation
            {
                Username = "TestUser",
                Timestamp = System.DateTime.UtcNow
            }
        };

        // Assert
        Assert.False(key.IsConfirmed);
    }

    [Fact]
    public void IsConfirmed_ReturnsFalseWhenBothMissing()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "" },
                { "es", "" }
            },
            ConfirmedBy = new Confirmation
            {
                Username = "TestUser",
                Timestamp = System.DateTime.UtcNow
            }
        };

        // Assert
        Assert.False(key.IsConfirmed);
    }
}
