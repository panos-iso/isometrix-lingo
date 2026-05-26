using System;

namespace IsometrixLingo.Models;

/// <summary>
/// Represents confirmation that a translation key has been reviewed and approved
/// </summary>
public class Confirmation
{
    /// <summary>
    /// Username of the person who confirmed the translation
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the translation was confirmed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Format the confirmation as a display string for UI
    /// </summary>
    public string DisplayText => $"Confirmed by {Username} on {Timestamp:MMM dd, yyyy}";

    /// <summary>
    /// Format the confirmation for file storage
    /// </summary>
    public string ToFileFormat() => $"CONFIRMED:by:[{Username}],at:[{Timestamp:yyyy-MM-ddTHH:mm:ssZ}]";

    /// <summary>
    /// Parse a confirmation from file format
    /// </summary>
    public static Confirmation? FromFileFormat(string formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted) || !formatted.StartsWith("CONFIRMED:"))
        {
            return null;
        }

        try
        {
            // Format: CONFIRMED:by:[username],at:[datetime]
            var content = formatted.Substring("CONFIRMED:".Length);
            var parts = content.Split(new[] { "by:[", "],at:[" }, StringSplitOptions.None);

            if (parts.Length != 3)
            {
                return null;
            }

            var username = parts[1];
            var timestampStr = parts[2].TrimEnd(']');

            if (!DateTime.TryParse(timestampStr, out var timestamp))
            {
                return null;
            }

            return new Confirmation
            {
                Username = username,
                Timestamp = timestamp
            };
        }
        catch
        {
            return null;
        }
    }
}
