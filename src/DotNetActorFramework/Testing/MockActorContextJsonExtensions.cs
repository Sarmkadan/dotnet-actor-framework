// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Testing;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="MockActorContext"/>.
/// Allows converting MockActorContext instances to and from JSON for testing scenarios.
/// </summary>
public static class MockActorContextJsonExtensions
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions PrettyPrintOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="MockActorContext"/> to a JSON string.
    /// </summary>
    /// <param name="value">The MockActorContext instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the MockActorContext.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this MockActorContext value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, indented ? PrettyPrintOptions : CamelCaseOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="MockActorContext"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized MockActorContext instance, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static MockActorContext? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            return JsonSerializer.Deserialize<MockActorContext>(json, CamelCaseOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="MockActorContext"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized MockActorContext if successful.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out MockActorContext? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        value = null;
        try
        {
            value = JsonSerializer.Deserialize<MockActorContext>(json, CamelCaseOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}