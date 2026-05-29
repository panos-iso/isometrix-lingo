using System;

namespace IsometrixLingo.Models;

/// <summary>
/// Represents a suggested translation value with attribution
/// </summary>
public class Suggestion
{
    /// <summary>
    /// The suggested translation value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Username of the person who made the suggestion
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the suggestion was made
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Format the suggestion as a display string for UI
    /// </summary>
    public string DisplayText => $"{Value} ({Username}, {Timestamp:MMM dd})";

    /// <summary>
    /// Format the suggestion for file storage with iso-lingo-audit prefix
    /// </summary>
    public string ToFileFormat() => $"iso-lingo-audit:SUGGESTION:{Value},by:[{Username}],at:[{Timestamp:yyyy-MM-ddTHH:mm:ssZ}]";

    /// <summary>
    /// Parse a suggestion from file format (expects iso-lingo-audit prefix)
    /// </summary>
    public static Suggestion? FromFileFormat(string formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted) || !formatted.StartsWith("iso-lingo-audit:SUGGESTION:"))
        {
            return null;
        }

        try
        {
            // Format: iso-lingo-audit:SUGGESTION:value,by:[username],at:[datetime]
            var content = formatted.Substring("iso-lingo-audit:SUGGESTION:".Length);
            var parts = content.Split(new[] { ",by:[", "],at:[" }, StringSplitOptions.None);

            if (parts.Length != 3)
            {
                return null;
            }

            var value = parts[0];
            var username = parts[1];
            var timestampStr = parts[2].TrimEnd(']');

            if (!DateTime.TryParse(timestampStr, out var timestamp))
            {
                return null;
            }

            return new Suggestion
            {
                Value = value,
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
