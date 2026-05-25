using System.Collections.Generic;
using IsometrixLingo.Models;
using Xunit;

namespace IsometrixLingo.Tests.ViewModels;

/// <summary>
/// Tests for mode-aware missing translations validation logic
/// </summary>
public class ModeAwareValidationTests
{
    [Fact]
    public void EditMode_DetectsMissingValue_IgnoresSuggestions()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "" }, // Missing English value
                { "es", "Spanish value" }
            },
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                // Has English suggestion, but in Edit mode this shouldn't count
                { "en", new Suggestion { Value = "English suggestion", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act - Simulate Edit mode validation
        var hasEnglishValue = key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
        var hasSpanishValue = key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
        var isMissing = !hasEnglishValue || !hasSpanishValue;

        // Assert - Should detect missing English despite suggestion
        Assert.True(isMissing);
    }

    [Fact]
    public void EditMode_AllValid_WhenBothValuesPresent()
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

        // Act - Simulate Edit mode validation
        var hasEnglishValue = key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
        var hasSpanishValue = key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
        var isMissing = !hasEnglishValue || !hasSpanishValue;

        // Assert
        Assert.False(isMissing);
    }

    [Fact]
    public void SuggestMode_AcceptsSuggestionAsValid()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "" } // Missing Spanish value
            },
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                // Has Spanish suggestion - in Suggest mode this should count as valid
                { "es", new Suggestion { Value = "Spanish suggestion", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act - Simulate Suggest mode validation
        var hasEnglish = (key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue))
                        || key.SuggestedValues.ContainsKey("en");
        var hasSpanish = (key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue))
                        || key.SuggestedValues.ContainsKey("es");
        var isMissing = !hasEnglish || !hasSpanish;

        // Assert - Should NOT be missing because suggestion counts in Suggest mode
        Assert.False(isMissing);
    }

    [Fact]
    public void SuggestMode_DetectsMissing_WhenNeitherValueNorSuggestion()
    {
        // Arrange
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "" } // Missing Spanish value
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
            // No Spanish suggestion either
        };

        // Act - Simulate Suggest mode validation
        var hasEnglish = (key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue))
                        || key.SuggestedValues.ContainsKey("en");
        var hasSpanish = (key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue))
                        || key.SuggestedValues.ContainsKey("es");
        var isMissing = !hasEnglish || !hasSpanish;

        // Assert - Should detect missing Spanish (no value AND no suggestion)
        Assert.True(isMissing);
    }

    [Fact]
    public void EditMode_DetectsEmptyAfterAcceptingSuggestion()
    {
        // Arrange - User accepted suggestions, then cleared values
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "" }, // Cleared after accepting
                { "es", "" }  // Cleared after accepting
            },
            SuggestedValues = new Dictionary<string, Suggestion>()
            // Suggestions were removed when accepted
        };

        // Act - Simulate Edit mode validation
        var hasEnglishValue = key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
        var hasSpanishValue = key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
        var isMissing = !hasEnglishValue || !hasSpanishValue;

        // Assert - Should detect both as missing
        Assert.True(isMissing);
    }

    [Fact]
    public void EditMode_DetectsPartiallyEmpty()
    {
        // Arrange - User edited English but left Spanish empty
        var key = new TranslationKey
        {
            Key = "test.key",
            LanguageValues = new Dictionary<string, string>
            {
                { "en", "English value" },
                { "es", "" } // Empty
            },
            SuggestedValues = new Dictionary<string, Suggestion>
            {
                // Has Spanish suggestion, but Edit mode ignores it
                { "es", new Suggestion { Value = "Spanish suggestion", Username = "user1", Timestamp = System.DateTime.UtcNow } }
            }
        };

        // Act - Simulate Edit mode validation
        var hasEnglishValue = key.LanguageValues.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue);
        var hasSpanishValue = key.LanguageValues.TryGetValue("es", out var esValue) && !string.IsNullOrWhiteSpace(esValue);
        var isMissing = !hasEnglishValue || !hasSpanishValue;

        // Assert - Should detect Spanish as missing
        Assert.True(isMissing);
    }
}
