// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Provides System.Text.Json serialization extensions for HttpActorClient.
/// </summary>
public static class HttpActorClientJsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the HttpActorClient instance to a JSON string.
    /// </summary>
    /// <param name="value">The HttpActorClient instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the HttpActorClient.</returns>
    public static string ToJson(this HttpActorClient value, bool indented = false)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an HttpActorClient instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An HttpActorClient instance, or null if deserialization fails.</returns>
    public static HttpActorClient? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<HttpActorClient>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an HttpActorClient instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting HttpActorClient instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out HttpActorClient? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<HttpActorClient>(json, JsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
