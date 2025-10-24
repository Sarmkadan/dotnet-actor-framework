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

    public byte[] Serialize(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var json = JsonSerializer.Serialize(message, Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

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
    byte[] Serialize(object state);
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

    public byte[] Serialize(object state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        var json = JsonSerializer.Serialize(state, state.GetType(), Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

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
    byte[] Serialize(Envelope envelope);
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

    public byte[] Serialize(Envelope envelope)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        var json = JsonSerializer.Serialize(envelope, Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

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
