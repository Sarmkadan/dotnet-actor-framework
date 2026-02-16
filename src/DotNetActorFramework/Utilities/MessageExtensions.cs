// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Utilities
{
    /// <summary>
    /// Extension methods for message inspection and validation.
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// Gets the age of a message in milliseconds since it was created.
        /// </summary>
        /// <param name="message">The message to calculate age for.</param>
        /// <returns>The age of the message in milliseconds.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
        public static long GetAge(this Message message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return (long)(DateTime.UtcNow - message.CreatedAt).TotalMilliseconds;
        }

        /// <summary>
        /// Determines if a message has exceeded the specified maximum age.
        /// </summary>
        /// <param name="message">The message to check for expiration.</param>
        /// <param name="timeout">The maximum allowed age for the message.</param>
        /// <returns><see langword="true"/> if the message has expired; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
        public static bool HasExpired(this Message message, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(message);
            return message.GetAge() > (long)timeout.TotalMilliseconds;
        }

        /// <summary>
        /// Checks whether the message has a non-empty identity.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns><see langword="true"/> if the message has a valid identity; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this Message message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return message.MessageId != Guid.Empty;
        }

        /// <summary>
        /// Returns a concise log-friendly representation of the message.
        /// </summary>
        /// <param name="message">The message to format for logging.</param>
        /// <returns>A formatted string representation of the message.</returns>
        public static string GetLogFormat(this Message message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return $"Message(Id={message.MessageId:N}, Type={message.GetType().Name}, Priority={message.Priority}, Age={message.GetAge()}ms)";
        }
    }
}