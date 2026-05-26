using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IsometrixLingo.Services;

/// <summary>
/// Service for handling GitHub Device Flow authentication
/// </summary>
public class GitHubOAuthService
{
    private const string DeviceCodeEndpoint = "https://github.com/login/device/code";
    private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
    private const string ClientId = "Iv23liI6hk4x6h8zRKfZ";
    
    public class DeviceCodeResponse
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string VerificationUri { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public int Interval { get; set; }
    }

    public GitHubOAuthService()
    {
    }

    /// <summary>
    /// Start the Device Flow and get an access token
    /// </summary>
    /// <param name="onCodeReceived">Callback when device code is received (userCode, verificationUri)</param>
    /// <returns>GitHub access token</returns>
    public async Task<string> AuthenticateAsync(Action<string, string> onCodeReceived)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        // Step 1: Request device code
        var deviceCodeRequest = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "scope", "user:email" }
        };

        var deviceCodeResponse = await httpClient.PostAsync(
            DeviceCodeEndpoint,
            new FormUrlEncodedContent(deviceCodeRequest));

        if (!deviceCodeResponse.IsSuccessStatusCode)
        {
            var errorContent = await deviceCodeResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to request device code: {errorContent}");
        }

        var deviceCodeJson = await deviceCodeResponse.Content.ReadAsStringAsync();
        var deviceCode = JsonSerializer.Deserialize<JsonElement>(deviceCodeJson);

        var userCode = deviceCode.GetProperty("user_code").GetString() 
            ?? throw new InvalidOperationException("No user code received");
        var verificationUri = deviceCode.GetProperty("verification_uri").GetString() 
            ?? throw new InvalidOperationException("No verification URI received");
        var deviceCodeValue = deviceCode.GetProperty("device_code").GetString() 
            ?? throw new InvalidOperationException("No device code received");
        var interval = deviceCode.GetProperty("interval").GetInt32();

        // Step 2: Show user the code
        onCodeReceived(userCode, verificationUri);

        // Step 3: Poll for access token
        while (true)
        {
            await Task.Delay(interval * 1000);

            var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "device_code", deviceCodeValue },
                { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
            };

            var tokenResponse = await httpClient.PostAsync(
                TokenEndpoint,
                new FormUrlEncodedContent(tokenRequest));

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            // Check for errors
            if (tokenData.TryGetProperty("error", out var error))
            {
                var errorCode = error.GetString();
                
                if (errorCode == "authorization_pending")
                {
                    // User hasn't authorized yet, continue polling
                    continue;
                }
                else if (errorCode == "slow_down")
                {
                    // Increase interval
                    interval += 5;
                    continue;
                }
                else if (errorCode == "expired_token")
                {
                    throw new InvalidOperationException("Device code expired. Please try again.");
                }
                else if (errorCode == "access_denied")
                {
                    throw new InvalidOperationException("User denied authorization.");
                }
                else
                {
                    var errorDescription = tokenData.TryGetProperty("error_description", out var desc)
                        ? desc.GetString()
                        : errorCode;
                    throw new InvalidOperationException($"Authorization failed: {errorDescription}");
                }
            }

            // Success!
            if (tokenData.TryGetProperty("access_token", out var accessToken))
            {
                return accessToken.GetString() 
                    ?? throw new InvalidOperationException("Access token is null");
            }
        }
    }
}
