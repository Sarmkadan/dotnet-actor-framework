// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Serialization;

/// <summary>
/// Extension methods for working with message envelopes.
/// </summary>
public static class EnvelopeExtensions
{
    /// <summary>
    /// Gets the elapsed time since the envelope was sent, in milliseconds.
    /// </summary>
    /// <param name="envelope">The envelope to calculate age for.</param>
    /// <returns>The elapsed time in milliseconds since the envelope was sent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is null.</exception>
    public static long GetAge(this Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return (long)(DateTime.UtcNow - envelope.SentAt).TotalMilliseconds;
    }

    /// <summary>
    /// Determines whether the envelope has been in transit longer than the given timeout.
    /// </summary>
    /// <param name="envelope">The envelope to check for expiration.</param>
    /// <param name="timeout">The maximum allowed transit time before expiration.</param>
    /// <returns>True if the envelope has been in transit longer than the timeout; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is null.</exception>
    public static bool HasExpired(this Envelope envelope, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        return envelope.GetAge() > timeout.TotalMilliseconds;
    }

    /// <summary>
    /// Returns a concise log-friendly representation of the envelope.
    /// </summary>
    /// <param name="envelope">The envelope to convert to a log string.</param>
    /// <returns>A string representation suitable for logging purposes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is null.</exception>
    public static string ToLogString(this Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return $"Envelope(Id={envelope.EnvelopeId:N}, Recipient={envelope.Recipient.Path}, " +
               $"MessageType={envelope.Message?.GetType().Name}, Age={envelope.GetAge()}ms)";
    }
}
