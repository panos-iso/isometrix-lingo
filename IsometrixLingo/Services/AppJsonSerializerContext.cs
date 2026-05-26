using System.Text.Json;
using System.Text.Json.Serialization;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

/// <summary>
/// JSON serializer context for source generation to support trimming
/// </summary>
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(SerializableSessionState))]
[JsonSerializable(typeof(SerializableSuggestion))]
[JsonSerializable(typeof(SerializableConfirmation))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IgnoreReadOnlyProperties = false,
    IncludeFields = false)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
