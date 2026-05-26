using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace IsometrixLingo.Services;

/// <summary>
/// Service for generating AI-powered translation suggestions using OpenAI/GitHub Models API
/// </summary>
public class AiTranslationService
{
    private const string DefaultEndpoint = "https://models.inference.ai.azure.com";
    private const string DefaultModel = "gpt-4o-mini";

    /// <summary>
    /// Translate text from English to Spanish using AI
    /// </summary>
    /// <param name="englishText">The English text to translate</param>
    /// <param name="apiToken">GitHub Models API token</param>
    /// <param name="model">Model to use (default: gpt-4o-mini)</param>
    /// <returns>The Spanish translation</returns>
    public async Task<string> TranslateToSpanishAsync(string englishText, string apiToken, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
            throw new InvalidOperationException("API token is required");

        try
        {
            var credential = new ApiKeyCredential(apiToken);
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(DefaultEndpoint)
            };
            var client = new ChatClient(model ?? DefaultModel, credential, options);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a professional translator. Translate the provided English text to Spanish. Return ONLY the Spanish translation, no explanations or additional text."),
                new UserChatMessage(englishText)
            };

            var response = await client.CompleteChatAsync(messages);

            if (response.Value?.Content == null || response.Value.Content.Count == 0)
                throw new InvalidOperationException("No translation received from AI");

            var translation = response.Value.Content[0].Text.Trim();
            return translation;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"AI translation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate both English and Spanish translations from a key name
    /// </summary>
    /// <param name="keyName">The translation key name</param>
    /// <param name="apiToken">GitHub Models API token</param>
    /// <param name="model">Model to use (default: gpt-4o-mini)</param>
    /// <returns>Tuple of (English text, Spanish text)</returns>
    public async Task<(string English, string Spanish)> GenerateFromKeyAsync(string keyName, string apiToken, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return (string.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(apiToken))
            throw new InvalidOperationException("API token is required");

        try
        {
            var credential = new ApiKeyCredential(apiToken);
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(DefaultEndpoint)
            };
            var client = new ChatClient(model ?? DefaultModel, credential, options);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a professional translator helping generate UI text from key names. Given a translation key, provide appropriate English and Spanish text. Return ONLY in this format: ENGLISH|SPANISH (e.g., 'Save Changes|Guardar Cambios'). No explanations."),
                new UserChatMessage($"Key: {keyName}")
            };

            var response = await client.CompleteChatAsync(messages);

            if (response.Value?.Content == null || response.Value.Content.Count == 0)
                throw new InvalidOperationException("No translation received from AI");

            var result = response.Value.Content[0].Text.Trim();
            var parts = result.Split('|');
            
            if (parts.Length >= 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }
            
            // Fallback if format is wrong
            return (result, string.Empty);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"AI translation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Batch translate multiple English texts to Spanish
    /// </summary>
    /// <param name="englishTexts">Dictionary of key-value pairs (key -> English text)</param>
    /// <param name="apiToken">GitHub Models API token</param>
    /// <param name="progressCallback">Optional callback for progress updates (current, total)</param>
    /// <param name="model">Model to use (default: gpt-4o-mini)</param>
    /// <returns>Dictionary of translations (key -> Spanish text)</returns>
    public async Task<Dictionary<string, string>> BatchTranslateAsync(
        Dictionary<string, string> englishTexts,
        string apiToken,
        Action<int, int>? progressCallback = null,
        string? model = null)
    {
        var translations = new Dictionary<string, string>();
        var items = englishTexts.ToList();
        var errors = new List<string>();
        
        for (int i = 0; i < items.Count; i++)
        {
            var (key, englishText) = items[i];
            
            try
            {
                var spanishText = await TranslateToSpanishAsync(englishText, apiToken, model);
                translations[key] = spanishText;
            }
            catch (Exception ex)
            {
                // Log error but continue with other translations
                errors.Add($"{key}: {ex.Message}");
                translations[key] = string.Empty;
            }

            progressCallback?.Invoke(i + 1, items.Count);
        }

        // If all translations failed, throw with first error
        if (errors.Count > 0 && translations.All(t => string.IsNullOrWhiteSpace(t.Value)))
        {
            throw new InvalidOperationException($"All translations failed. First error: {errors[0]}");
        }

        return translations;
    }

    /// <summary>
    /// Batch generate both English and Spanish translations from key names
    /// </summary>
    /// <param name="keyNames">List of translation key names</param>
    /// <param name="apiToken">GitHub Models API token</param>
    /// <param name="progressCallback">Optional callback for progress updates (current, total)</param>
    /// <param name="model">Model to use (default: gpt-4o-mini)</param>
    /// <returns>Dictionary of translations (key -> (English, Spanish))</returns>
    public async Task<Dictionary<string, (string English, string Spanish)>> BatchGenerateFromKeysAsync(
        List<string> keyNames,
        string apiToken,
        Action<int, int>? progressCallback = null,
        string? model = null)
    {
        var translations = new Dictionary<string, (string English, string Spanish)>();
        var errors = new List<string>();
        
        for (int i = 0; i < keyNames.Count; i++)
        {
            var keyName = keyNames[i];
            
            try
            {
                var (english, spanish) = await GenerateFromKeyAsync(keyName, apiToken, model);
                translations[keyName] = (english, spanish);
            }
            catch (Exception ex)
            {
                // Log error but continue with other translations
                errors.Add($"{keyName}: {ex.Message}");
                translations[keyName] = (string.Empty, string.Empty);
            }

            progressCallback?.Invoke(i + 1, keyNames.Count);
        }

        // If all translations failed, throw with first error
        if (errors.Count > 0 && translations.All(t => string.IsNullOrWhiteSpace(t.Value.English) && string.IsNullOrWhiteSpace(t.Value.Spanish)))
        {
            throw new InvalidOperationException($"All translations failed. First error: {errors[0]}");
        }

        return translations;
    }
}
