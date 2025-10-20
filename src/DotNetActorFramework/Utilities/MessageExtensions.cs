// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Extension methods for message creation and manipulation.
/// Provides fluent builders and helper methods to simplify message creation throughout the framework.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Creates a new message with the specified payload and type.
    /// </summary>
    public static Message Create(string type, object payload)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Message type cannot be empty.", nameof(type));

        return new Message
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            Priority = MessagePriority.Normal
        };
    }

    /// <summary>
    /// Creates a new high-priority message.
    /// </summary>
    public static Message CreateHighPriority(string type, object payload)
    {
        var msg = Create(type, payload);
        msg.Priority = MessagePriority.High;
        return msg;
    }

    /// <summary>
    /// Creates a new low-priority message.
    /// </summary>
    public static Message CreateLowPriority(string type, object payload)
    {
        var msg = Create(type, payload);
        msg.Priority = MessagePriority.Low;
        return msg;
    }

    /// <summary>
    /// Sets custom metadata on a message.
    /// </summary>
    public static Message WithMetadata(this Message message, string key, string value)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Metadata key cannot be empty.", nameof(key));

        message.Metadata ??= [];
        message.Metadata[key] = value;
        return message;
    }

    /// <summary>
    /// Gets metadata value from a message, returning null if not found.
    /// </summary>
    public static string? GetMetadata(this Message message, string key)
    {
        if (message?.Metadata == null) return null;
        message.Metadata.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Gets the age of a message in milliseconds.
    /// </summary>
    public static long GetAge(this Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return (long)(DateTime.UtcNow - message.CreatedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Determines if a message has expired based on the specified timeout.
    /// </summary>
    public static bool HasExpired(this Message message, TimeSpan timeout)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.GetAge() > (long)timeout.TotalMilliseconds;
    }

    /// <summary>
    /// Gets a string representation of the message suitable for logging.
    /// </summary>
    public static string GetLogFormat(this Message message)
    {
        if (message == null) return "null";
        return $"Message(Id={message.Id:N}, Type={message.Type}, Priority={message.Priority}, Age={message.GetAge()}ms)";
    }

    /// <summary>
    /// Validates that a message has required properties set.
    /// </summary>
    public static bool IsValid(this Message message)
    {
        if (message == null) return false;
        if (message.Id == Guid.Empty) return false;
        if (string.IsNullOrWhiteSpace(message.Type)) return false;
        return true;
    }
}
