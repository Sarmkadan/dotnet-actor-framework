// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Serialization and deserialization utilities for messages and state.
/// Provides convenient methods for converting objects to/from JSON with proper error handling.
/// </summary>
public static class SerializationExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A JSON string representation of the object, or "null" if the object is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null and the type is a value type.</exception>
    public static string ToJson<T>(this T obj) =>
        obj is null
            ? "null"
            : JsonSerializer.Serialize(obj, DefaultOptions);

    /// <summary>
    /// Serializes an object to pretty-printed JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A pretty-printed JSON string representation of the object, or "null" if the object is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null and the type is a value type.</exception>
    public static string ToJsonPretty<T>(this T obj) =>
        obj is null
            ? "null"
            : JsonSerializer.Serialize(obj, PrettyOptions);

    /// <summary>
    /// Serializes an object to JSON bytes.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A UTF-8 encoded JSON byte array representation of the object, or an empty array if the object is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null and the type is a value type.</exception>
    public static byte[] ToJsonBytes<T>(this T obj) =>
        obj is null
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj, DefaultOptions));

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails or input is null/empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized to type <typeparamref name="T"/>.</exception>
    public static T? FromJson<T>(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrEmpty(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Deserializes JSON bytes to an object.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="data">The JSON byte array to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails or input is null/empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON bytes cannot be deserialized to type <typeparamref name="T"/>.</exception>
    public static T? FromJsonBytes<T>(this byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
            return default;

        try
        {
            var json = Encoding.UTF8.GetString(data);
            return json.FromJson<T>();
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize JSON bytes", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize JSON with error information.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">The deserialized result, or null if deserialization fails.</param>
    /// <param name="error">Error message if deserialization fails, otherwise null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    public static bool TryFromJson<T>(this string json, out T? result, out string? error)
    {
        ArgumentNullException.ThrowIfNull(json);

        result = default;
        error = null;

        if (string.IsNullOrEmpty(json))
        {
            error = "JSON string is null or empty";
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON deserialization failed: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Unexpected error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Determines if a string is valid JSON.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>True if the string is valid JSON; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    public static bool IsValidJson(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrEmpty(json))
            return false;

        try
        {
            JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a deep copy of an object by serializing and deserializing it.
    /// </summary>
    /// <typeparam name="T">The type of the object to deep copy.</typeparam>
    /// <param name="obj">The object to deep copy.</param>
    /// <returns>A deep copy of the object, or null if the input is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is null and the type is a value type.</exception>
    public static T? DeepCopy<T>(this T obj) =>
        obj is null
            ? default
            : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj, DefaultOptions), DefaultOptions);
}
