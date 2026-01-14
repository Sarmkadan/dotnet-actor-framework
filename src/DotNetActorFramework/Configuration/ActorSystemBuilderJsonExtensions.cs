// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DotNetActorFramework.Middleware;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ActorSystemBuilder"/>.
/// </summary>
public static class ActorSystemBuilderJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Converts the <see cref="ActorSystemBuilder"/> to a JSON string representation.
    /// </summary>
    /// <param name="value">The actor system builder to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this ActorSystemBuilder value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        // Serialize the builder state by capturing its configuration
        var builderState = new ActorSystemBuilderState
        {
            SystemName = value.GetType().GetField("_systemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) as string ?? string.Empty,
            Options = value.GetType().GetProperty("Options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) as ActorSystemOptions ?? new ActorSystemOptions(),
            Middleware = value.GetType().GetField("_middleware", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) as System.Collections.Generic.List<IActorMiddleware> ?? new System.Collections.Generic.List<IActorMiddleware>()
        };

        return JsonSerializer.Serialize(builderState, options);
    }

    /// <summary>
    /// Deserializes an <see cref="ActorSystemBuilder"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="ActorSystemBuilder"/> instance, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    public static ActorSystemBuilder? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            var builderState = JsonSerializer.Deserialize<ActorSystemBuilderState>(json, _jsonSerializerOptions);
            if (builderState == null)
            {
                return null;
            }

            var builder = new ActorSystemBuilder(builderState.SystemName ?? "DeserializedActorSystem");

            // Apply the options if they exist
            if (builderState.Options != null)
            {
                // Options are set during construction, so we need to apply them via reflection
                var optionsProperty = builder.GetType().GetProperty("Options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (optionsProperty != null)
                {
                    optionsProperty.SetValue(builder, builderState.Options);
                }
            }

            // Add middleware if they exist
            if (builderState.Middleware != null && builderState.Middleware.Count > 0)
            {
                var middlewareField = builder.GetType().GetField("_middleware", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (middlewareField != null)
                {
                    var middlewareList = middlewareField.GetValue(builder) as System.Collections.Generic.List<IActorMiddleware>;
                    if (middlewareList != null)
                    {
                        foreach (var middleware in builderState.Middleware)
                        {
                            middlewareList.Add(middleware);
                        }
                    }
                }
            }

            return builder;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize an <see cref="ActorSystemBuilder"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized builder, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out ActorSystemBuilder? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = FromJson(json);
            return value != null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Represents the serializable state of an <see cref="ActorSystemBuilder"/>.
    /// </summary>
    private sealed class ActorSystemBuilderState
    {
        public string? SystemName { get; set; }
        public ActorSystemOptions? Options { get; set; }
        public System.Collections.Generic.List<IActorMiddleware>? Middleware { get; set; }
    }
}