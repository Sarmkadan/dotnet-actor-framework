// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Extension methods for message inspection and validation.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Gets the age of a message in milliseconds since it was created.
    /// </summary>
    public static long GetAge(this Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return (long)(DateTime.UtcNow - message.CreatedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Determines if a message has exceeded the specified maximum age.
    /// </summary>
    public static bool HasExpired(this Message message, TimeSpan timeout)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.GetAge() > (long)timeout.TotalMilliseconds;
    }

    /// <summary>
    /// Checks whether the message has a non-empty identity.
    /// </summary>
    public static bool IsValid(this Message message)
    {
        if (message == null) return false;
        return message.MessageId != Guid.Empty;
    }

    /// <summary>
    /// Returns a concise log-friendly representation of the message.
    /// </summary>
    public static string GetLogFormat(this Message message)
    {
        if (message == null) return "null";
        return $"Message(Id={message.MessageId:N}, Type={message.GetType().Name}, Priority={message.Priority}, Age={message.GetAge()}ms)";
    }
}
