// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Serialization;

/// <summary>
/// Interface for message serialization/deserialization.
/// Allows different serialization strategies for different message types.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes a message to bytes.
    /// </summary>
    byte[] Serialize(Message message);

    /// <summary>
    /// Deserializes a message from bytes.
    /// </summary>
    Message? Deserialize(byte[] data);
}

/// <summary>
/// JSON-based message serializer using System.Text.Json.
/// Provides efficient and standard JSON serialization for messages.
/// </summary>
public class JsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a message to UTF-8 encoded JSON bytes.
    /// </summary>
    /// <param name="message">The message to serialize. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public byte[] Serialize(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var json = JsonSerializer.Serialize(message, Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes a message from UTF-8 encoded JSON bytes.
    /// Returns null when the data is null, empty, or not valid JSON.
    /// </summary>
    /// <param name="data">The bytes to deserialize.</param>
    public Message? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
            return null;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<Message>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Serializer for actor state objects.
/// Handles serialization of actor internal state for persistence and transmission.
/// </summary>
public interface IStateSerializer
{
    /// <summary>
    /// Serializes an actor state object to bytes.
    /// </summary>
    byte[] Serialize(object state);

    /// <summary>
    /// Deserializes actor state of type <typeparamref name="T"/> from bytes.
    /// </summary>
    T? Deserialize<T>(byte[] data);
}

/// <summary>
/// JSON-based state serializer.
/// </summary>
public class JsonStateSerializer : IStateSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an actor state object to UTF-8 encoded JSON bytes, using the runtime type of the state.
    /// </summary>
    /// <param name="state">The state object to serialize. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is null.</exception>
    public byte[] Serialize(object state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var json = JsonSerializer.Serialize(state, state.GetType(), Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes actor state of type <typeparamref name="T"/> from UTF-8 encoded JSON bytes.
    /// Returns the default value of <typeparamref name="T"/> when the data is null, empty, or not valid JSON.
    /// </summary>
    /// <typeparam name="T">The type of state to deserialize.</typeparam>
    /// <param name="data">The bytes to deserialize.</param>
    public T? Deserialize<T>(byte[] data)
    {
        if (data == null || data.Length == 0)
            return default;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}

/// <summary>
/// Serializer for envelopes containing messages.
/// </summary>
public interface IEnvelopeSerializer
{
    /// <summary>
    /// Serializes an envelope to bytes.
    /// </summary>
    byte[] Serialize(Envelope envelope);

    /// <summary>
    /// Deserializes an envelope from bytes.
    /// </summary>
    Envelope? Deserialize(byte[] data);
}

/// <summary>
/// JSON-based envelope serializer.
/// </summary>
public class JsonEnvelopeSerializer : IEnvelopeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an envelope to UTF-8 encoded JSON bytes.
    /// </summary>
    /// <param name="envelope">The envelope to serialize. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is null.</exception>
    public byte[] Serialize(Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var json = JsonSerializer.Serialize(envelope, Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes an envelope from UTF-8 encoded JSON bytes.
    /// Returns null when the data is null, empty, or not valid JSON.
    /// </summary>
    /// <param name="data">The bytes to deserialize.</param>
    public Envelope? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
            return null;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<Envelope>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
