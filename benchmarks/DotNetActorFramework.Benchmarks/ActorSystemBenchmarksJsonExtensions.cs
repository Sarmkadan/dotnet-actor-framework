using System.Text.Json;

namespace DotNetActorFramework.Benchmarks;

/// <summary>
/// Provides System.Text.Json serialization extensions for ActorSystemBenchmarks.
/// </summary>
public static class ActorSystemBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new()
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
    public static string ToJson(this ActorSystemBenchmarks value, bool indented = false)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var options = indented
            ? new JsonSerializerOptions(Options)
            {
                WriteIndented = true
            }
            : Options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes an ActorSystemBenchmarks instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized ActorSystemBenchmarks instance, or null if the JSON is invalid.</returns>
    public static ActorSystemBenchmarks? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ActorSystemBenchmarks>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize an ActorSystemBenchmarks instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized ActorSystemBenchmarks instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out ActorSystemBenchmarks? value)
    {
        value = FromJson(json);
        return value != null;
    }
}