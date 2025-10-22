// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static long GetAge(this Envelope envelope)
    {
        if (envelope == null) return 0;
        return (long)(DateTime.UtcNow - envelope.SentAt).TotalMilliseconds;
    }

    /// <summary>
    /// Determines whether the envelope has been in transit longer than the given timeout.
    /// </summary>
    public static bool HasExpired(this Envelope envelope, TimeSpan timeout)
    {
        if (envelope == null) return false;
        return envelope.GetAge() > (long)timeout.TotalMilliseconds;
    }

    /// <summary>
    /// Returns a concise log-friendly representation of the envelope.
    /// </summary>
    public static string ToLogString(this Envelope envelope)
    {
        if (envelope == null) return "null";
        return $"Envelope(Id={envelope.EnvelopeId:N}, Recipient={envelope.Recipient.Path}, " +
               $"MessageType={envelope.Message?.GetType().Name}, Age={envelope.GetAge()}ms)";
    }
}
