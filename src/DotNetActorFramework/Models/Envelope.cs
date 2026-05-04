// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

/// <summary>
/// Wraps a message with metadata about sender and recipient.
/// </summary>
public class Envelope
{
    public Message Message { get; }
    public ActorRef? Sender { get; }
    public ActorRef Recipient { get; }
    public DateTime SentAt { get; }
    public Guid EnvelopeId { get; }
    public int RetryCount { get; private set; }
    public bool IsDelivered { get; private set; }

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
