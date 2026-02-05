// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace DotNetActorFramework.Models;

/// <summary>
/// Provides extension methods for <see cref="Envelope"/> to enhance message handling capabilities.
/// </summary>
public static class EnvelopeExtensions
{
    /// <summary>
    /// Determines whether the envelope contains a message of the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to check for.</typeparam>
    /// <param name="envelope">The envelope instance.</param>
    /// <returns><see langword="true"/> if the message is of type T; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is <see langword="null"/>.</exception>
    public static bool IsMessageType<T>(this Envelope envelope) where T : Message
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return envelope.Message is T;
    }

    /// <summary>
    /// Attempts to cast the message to the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to cast to.</typeparam>
    /// <param name="envelope">The envelope instance.</param>
    /// <param name="message">When this method returns, contains the message if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the cast was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is <see langword="null"/>.</exception>
    public static bool TryGetMessage<T>(this Envelope envelope, out T? message) where T : Message
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.Message is T typedMessage)
        {
            message = typedMessage;
            return true;
        }

        message = null;
        return false;
    }

    /// <summary>
    /// Creates a shallow copy of the envelope with a new unique identifier.
    /// Note: The message is not deep-copied as Message types are typically immutable.
    /// </summary>
    /// <param name="envelope">The envelope instance.</param>
    /// <returns>A new envelope instance with the same message content but new metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is <see langword="null"/>.</exception>
    public static Envelope Clone(this Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new Envelope(envelope.Message, envelope.Recipient, envelope.Sender)
        {
            // RetryCount and IsDelivered are initialized to defaults (0 and false) in constructor
        };
    }

    /// <summary>
    /// Formats the envelope for logging purposes with detailed information.
    /// </summary>
    /// <param name="envelope">The envelope instance.</param>
    /// <param name="includeMessageContent">Whether to include message content in the output.</param>
    /// <returns>A formatted string representation of the envelope.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is <see langword="null"/>.</exception>
    public static string ToLogString(this Envelope envelope, bool includeMessageContent = false)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var builder = new System.Text.StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Envelope: {envelope.EnvelopeId:N}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Sent: {envelope.SentAt:O}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Recipient: {envelope.Recipient.Path.Name}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Sender: {(envelope.Sender?.Path.Name ?? "System")}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Retry Count: {envelope.RetryCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Delivered: {envelope.IsDelivered}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Priority: {envelope.GetDeliveryPriority()}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Elapsed: {envelope.GetElapsedTime().TotalMilliseconds:F2}ms");

        if (includeMessageContent)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Message Content:");
            builder.AppendLine(envelope.Message.ToString());
        }
        else
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Message Type: {envelope.Message.GetType().Name}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the age of the envelope in a human-readable format.
    /// </summary>
    /// <param name="envelope">The envelope instance.</param>
    /// <returns>A formatted string representing the envelope's age.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is <see langword="null"/>.</exception>
    public static string GetAgeString(this Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var elapsed = envelope.GetElapsedTime();
        return elapsed.TotalSeconds switch
        {
            < 1 => "just now",
            < 60 => $"{elapsed.TotalSeconds:F0}s ago",
            < 3600 => $"{elapsed.TotalMinutes:F0}m ago",
            < 86400 => $"{elapsed.TotalHours:F1}h ago",
            _ => $"{elapsed.TotalDays:F1}d ago"
        };
    }
}