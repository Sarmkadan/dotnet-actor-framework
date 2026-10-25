// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

/// <summary>
/// Wraps a message with metadata about sender and recipient.
///
/// <para>This class uses public fields for RetryCount and IsDelivered to avoid the overhead
/// of property accessors on the hot send path while maintaining heap allocation (safer than struct).
/// The fields are intentionally public to allow the MessageDispatcher and LoadBasedRouter
/// to update delivery state after creation without requiring object initializers.</para>
/// </summary>
public class Envelope
{
    /// <summary>
    /// The message being delivered.
    /// </summary>
    public readonly Message Message;

    /// <summary>
    /// The actor that sent the message, or <c>null</c> if it was sent by the system.
    /// </summary>
    public readonly ActorRef? Sender;

    /// <summary>
    /// The actor the message is addressed to.
    /// </summary>
    public readonly ActorRef Recipient;

    /// <summary>
    /// The UTC timestamp at which the envelope was created.
    /// </summary>
    public readonly DateTime SentAt;

    /// <summary>
    /// The unique identifier of this envelope.
    /// </summary>
    public readonly Guid EnvelopeId;

    // Mutable state for delivery tracking - intentionally public fields to avoid allocations
    // and property accessor overhead on the hot path

    /// <summary>
    /// The number of failed delivery attempts made for this envelope.
    /// </summary>
    public int RetryCount;

    /// <summary>
    /// Indicates whether this envelope has been successfully delivered.
    /// </summary>
    public bool IsDelivered;

    /// <summary>
    /// Initializes a new envelope for the specified message and recipient.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="recipient">The actor the message is addressed to.</param>
    /// <param name="sender">The actor that sent the message, or <c>null</c> for system messages.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="recipient"/> is <c>null</c>.</exception>
    public Envelope(Message message, ActorRef recipient, ActorRef? sender = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(recipient);
        Message = message;
        Recipient = recipient;
        Sender = sender;
        SentAt = DateTime.UtcNow;
        EnvelopeId = Guid.NewGuid();
        RetryCount = 0;
        IsDelivered = false;
    }

    /// <summary>
    /// Marks this envelope as delivered.
    /// </summary>
    public void MarkAsDelivered() => IsDelivered = true;

    /// <summary>
    /// Increments the retry count for failed delivery attempts.
    /// </summary>
    public void IncrementRetryCount() => RetryCount++;

    /// <summary>
    /// Gets the time elapsed since this message was sent.
    /// </summary>
    public TimeSpan GetElapsedTime() => DateTime.UtcNow - SentAt;

    /// <summary>
    /// Checks if this envelope has exceeded the retry limit.
    /// </summary>
    public bool HasExceededRetryLimit(int maxRetries = 3) => RetryCount > maxRetries;

    /// <summary>
    /// Gets priority-adjusted delivery order information.
    /// </summary>
    public int GetDeliveryPriority()
    {
        return Message.Priority * 100 + (int)(GetElapsedTime().TotalMilliseconds / 1000);
    }

    /// <summary>
    /// Returns a short human-readable description of this envelope for diagnostics.
    /// </summary>
    /// <returns>A string in the form <c>[id] sender -> recipient: MessageType</c>.</returns>
    public override string ToString()
    {
        var senderInfo = Sender?.Path.Name ?? "System";
        return $"[{EnvelopeId:N}] {senderInfo} -> {Recipient.Path.Name}: {Message.GetType().Name}";
    }
}
