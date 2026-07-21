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
    public readonly Message Message;
    public readonly ActorRef? Sender;
    public readonly ActorRef Recipient;
    public readonly DateTime SentAt;
    public readonly Guid EnvelopeId;

    // Mutable state for delivery tracking - intentionally public fields to avoid allocations
    // and property accessor overhead on the hot path
    public int RetryCount;
    public bool IsDelivered;

    public Envelope(Message message, ActorRef recipient, ActorRef? sender = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
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

    public override string ToString()
    {
        var senderInfo = Sender?.Path.Name ?? "System";
        return $"[{EnvelopeId:N}] {senderInfo} -> {Recipient.Path.Name}: {Message.GetType().Name}";
    }
}
