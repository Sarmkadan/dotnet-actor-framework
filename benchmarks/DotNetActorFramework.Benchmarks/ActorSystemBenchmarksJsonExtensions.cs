using System.Text.Json;

namespace DotNetActorFramework.Benchmarks;

/// <summary>
/// Provides System.Text.Json serialization extensions for ActorSystemBenchmarks.
/// </summary>
public static class ActorSystemBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the ActorSystemBenchmarks instance to a JSON string.
    /// </summary>
    /// <param name="value">The ActorSystemBenchmarks instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the ActorSystemBenchmarks instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ActorSystemBenchmarks value, bool indented = false) =>
        value is null
            ? throw new ArgumentNullException(nameof(value))
            : JsonSerializer.Serialize(value, GetOptions(indented));

    /// <summary>
    /// Deserializes an ActorSystemBenchmarks instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized ActorSystemBenchmarks instance, or null if the JSON is invalid or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static ActorSystemBenchmarks? FromJson(string? json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return TryDeserialize(json, out var result) ? result : null;
    }

    /// <summary>
    /// Attempts to deserialize an ActorSystemBenchmarks instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized ActorSystemBenchmarks instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string? json, out ActorSystemBenchmarks? value)
    {
        value = null;

        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        return TryDeserialize(json, out value);
    }

    private static JsonSerializerOptions GetOptions(bool indented) =>
        indented
            ? new JsonSerializerOptions(Options) { WriteIndented = true }
            : Options;

    private static bool TryDeserialize(string json, out ActorSystemBenchmarks? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<ActorSystemBenchmarks>(json, Options);
            return result is not null;
        }
        catch (JsonException)
        {
            result = null;
            return false;
        }
    }
}