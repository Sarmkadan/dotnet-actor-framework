// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="HttpActorClient"/>.
/// </summary>
public static class HttpActorClientJsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the <see cref="HttpActorClient"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="HttpActorClient"/> instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the <see cref="HttpActorClient"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this HttpActorClient value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? JsonSerializerOptions.Web : JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an <see cref="HttpActorClient"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="HttpActorClient"/> instance, or <see langword="null"/> if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static HttpActorClient? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

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
    /// Attempts to deserialize a JSON string to an <see cref="HttpActorClient"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting <see cref="HttpActorClient"/> instance, or <see langword="null"/> if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out HttpActorClient? value)
    {
        ArgumentNullException.ThrowIfNull(json);

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