// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// System.Text.Json serialization marker for GuardExtensions validation patterns.
/// GuardExtensions is a static class containing extension methods for validation.
/// This class provides methods to serialize/deserialize guard clause configuration
/// rather than the static class itself, which cannot be instantiated.
/// </summary>
public static class GuardExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes guard clause validation patterns to a JSON string.
    /// </summary>
    /// <param name="guardPattern">A delegate representing a guard clause validation pattern.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the guard clause validation pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown if guardPattern is null.</exception>
    public static string ToJson(this Delegate guardPattern, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(guardPattern);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(guardPattern.Method.Name, options);
    }

    /// <summary>
    /// Deserializes a guard clause validation pattern from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the validation pattern name.</param>
    /// <returns>A delegate representing the guard clause validation pattern, or null.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or contains an unknown pattern.</exception>
    public static Delegate? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        string patternName = JsonSerializer.Deserialize<string>(json, _jsonSerializerOptions) ?? string.Empty;

        return patternName switch
        {
            "NotNullOrEmpty" => GuardExtensions.NotNullOrEmpty,
            "NotNullOrWhiteSpace" => GuardExtensions.NotNullOrWhiteSpace,
            _ => throw new JsonException($"Unknown guard pattern: {patternName}")
        };
    }

    /// <summary>
    /// Attempts to deserialize a guard clause validation pattern from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the validation pattern name.</param>
    /// <param name="value">Receives the deserialized guard clause validation pattern if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out Delegate? value)
    {
        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return true;
        }

        try
        {
            string patternName = JsonSerializer.Deserialize<string>(json, _jsonSerializerOptions) ?? string.Empty;

            value = patternName switch
            {
                "NotNullOrEmpty" => GuardExtensions.NotNullOrEmpty,
                "NotNullOrWhiteSpace" => GuardExtensions.NotNullOrWhiteSpace,
                _ => null
            };

            return value != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}