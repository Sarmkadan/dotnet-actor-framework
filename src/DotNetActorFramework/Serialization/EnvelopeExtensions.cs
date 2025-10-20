// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Serialization;

/// <summary>
/// Extension methods for working with message envelopes.
/// Simplifies envelope manipulation and metadata handling.
/// </summary>
public static class EnvelopeExtensions
{
    /// <summary>
    /// Creates a new envelope with the specified message and recipient.
    /// </summary>
    public static Envelope Create(ActorPath recipientPath, Message message)
    {
        if (recipientPath == null || message == null)
            throw new ArgumentNullException();

        return new Envelope
        {
            Id = Guid.NewGuid(),
            RecipientPath = recipientPath,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            Metadata = []
        };
    }

    /// <summary>
    /// Sets metadata on an envelope.
    /// </summary>
    public static Envelope WithMetadata(this Envelope envelope, string key, string value)
    {
        if (envelope?.Metadata == null) return envelope;
        envelope.Metadata[key] = value;
        return envelope;
    }

    /// <summary>
    /// Gets metadata value from an envelope.
    /// </summary>
    public static string? GetMetadata(this Envelope envelope, string key)
    {
        if (envelope?.Metadata == null) return null;
        envelope.Metadata.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Sets the sender ID in the envelope.
    /// </summary>
    public static Envelope WithSender(this Envelope envelope, string senderId)
    {
        return envelope.WithMetadata("sender-id", senderId);
    }

    /// <summary>
    /// Gets the sender ID from the envelope.
    /// </summary>
    public static string? GetSenderId(this Envelope envelope)
    {
        return envelope.GetMetadata("sender-id");
    }

    /// <summary>
    /// Sets a trace ID for distributed tracing.
    /// </summary>
    public static Envelope WithTraceId(this Envelope envelope, string traceId)
    {
        return envelope.WithMetadata("trace-id", traceId);
    }

    /// <summary>
    /// Gets the trace ID from the envelope.
    /// </summary>
    public static string? GetTraceId(this Envelope envelope)
    {
        return envelope.GetMetadata("trace-id");
    }

    /// <summary>
    /// Sets a correlation ID for tracking related messages.
    /// </summary>
    public static Envelope WithCorrelationId(this Envelope envelope, string correlationId)
    {
        return envelope.WithMetadata("correlation-id", correlationId);
    }

    /// <summary>
    /// Gets the correlation ID from the envelope.
    /// </summary>
    public static string? GetCorrelationId(this Envelope envelope)
    {
        return envelope.GetMetadata("correlation-id");
    }

    /// <summary>
    /// Gets the age of the envelope in milliseconds.
    /// </summary>
    public static long GetAge(this Envelope envelope)
    {
        if (envelope == null) return 0;
        return (long)(DateTime.UtcNow - envelope.CreatedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Determines if an envelope has expired based on the specified timeout.
    /// </summary>
    public static bool HasExpired(this Envelope envelope, TimeSpan timeout)
    {
        if (envelope == null) return false;
        return envelope.GetAge() > (long)timeout.TotalMilliseconds;
    }

    /// <summary>
    /// Creates a JSON representation of the envelope suitable for logging.
    /// </summary>
    public static string ToLogString(this Envelope envelope)
    {
        if (envelope == null) return "null";

        return $"Envelope(Id={envelope.Id:N}, RecipientPath={envelope.RecipientPath}, " +
               $"MessageType={envelope.Message?.Type}, Age={envelope.GetAge()}ms)";
    }

    /// <summary>
    /// Creates a reply envelope with the same recipient and metadata.
    /// </summary>
    public static Envelope CreateReply(this Envelope envelope, Message replyMessage)
    {
        if (envelope == null || replyMessage == null)
            throw new ArgumentNullException();

        var reply = Create(envelope.RecipientPath, replyMessage);

        // Copy correlation IDs for traceability
        var traceId = envelope.GetTraceId();
        if (!string.IsNullOrEmpty(traceId))
            reply.WithTraceId(traceId);

        var correlationId = envelope.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
            reply.WithCorrelationId(correlationId);

        return reply;
    }
}
