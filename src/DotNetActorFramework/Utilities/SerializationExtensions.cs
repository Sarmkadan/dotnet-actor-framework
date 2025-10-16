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
    public static string ToJson<T>(this T obj)
    {
        if (obj == null) return "null";
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    /// <summary>
    /// Serializes an object to pretty-printed JSON string.
    /// </summary>
    public static string ToJsonPretty<T>(this T obj)
    {
        if (obj == null) return "null";
        return JsonSerializer.Serialize(obj, PrettyOptions);
    }

    /// <summary>
    /// Serializes an object to JSON bytes.
    /// </summary>
    public static byte[] ToJsonBytes<T>(this T obj)
    {
        if (obj == null) return [];
        var json = obj.ToJson();
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// Returns null if deserialization fails or input is null.
    /// </summary>
    public static T? FromJson<T>(this string json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes JSON bytes to an object.
    /// Returns null if deserialization fails or input is empty.
    /// </summary>
    public static T? FromJsonBytes<T>(this byte[] data)
    {
        if (data == null || data.Length == 0) return default;
        try
        {
            var json = Encoding.UTF8.GetString(data);
            return json.FromJson<T>();
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Attempts to deserialize JSON with error information.
    /// </summary>
    public static bool TryFromJson<T>(this string json, out T? result, out string? error)
    {
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
    public static bool IsValidJson(this string json)
    {
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            JsonDocument.Parse(json);
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
    public static T? DeepCopy<T>(this T obj)
    {
        if (obj == null) return default;
        var json = obj.ToJson();
        return json.FromJson<T>();
    }
}
