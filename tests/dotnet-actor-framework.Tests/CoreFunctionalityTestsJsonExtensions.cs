// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for CoreFunctionalityTests.
/// </summary>
namespace DotNetActorFramework.Tests;

public static class CoreFunctionalityTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes the CoreFunctionalityTests instance to a JSON string.
    /// </summary>
    /// <param name="value">The CoreFunctionalityTests instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the CoreFunctionalityTests instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this CoreFunctionalityTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a CoreFunctionalityTests instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A CoreFunctionalityTests instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static CoreFunctionalityTests? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CoreFunctionalityTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a CoreFunctionalityTests instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized CoreFunctionalityTests instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out CoreFunctionalityTests? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        value = null;

        try
        {
            value = JsonSerializer.Deserialize<CoreFunctionalityTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}