using System;
using System.Collections.Generic;

namespace DotNetActorFramework.Models;

/// <summary>
/// Extension methods for <see cref="Message"/> providing header handling and expiration checks.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Returns a new message instance with the specified header added or replaced.
    /// The original message remains unchanged because records are immutable.
    /// </summary>
    /// <typeparam name="TMessage">The concrete message type.</typeparam>
    /// <param name="message">The source message.</param>
    /// <param name="key">Header key. Must be non‑null, non‑empty.</param>
    /// <param name="value">Header value.</param>
    /// <returns>A new message with the header applied.</returns>
    public static TMessage WithHeader<TMessage>(this TMessage message, string key, object value)
        where TMessage : Message
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Header key cannot be null or whitespace.", nameof(key));

        var newHeaders = new Dictionary<string, object>(message.Headers) { [key] = value };
        return message with { Headers = newHeaders };
    }

    /// <summary>
    /// Retrieves a header value cast to <typeparamref name="T"/> if present; otherwise returns <paramref name="defaultValue"/>.
    /// </summary>
    /// <typeparam name="T">Expected type of the header value.</typeparam>
    /// <param name="message">The message containing headers.</param>
    /// <param name="key">Header key.</param>
    /// <param name="defaultValue">Value to return when the header is missing or cannot be cast.</param>
    /// <returns>The header value cast to <typeparamref name="T"/> or <paramref name="defaultValue"/>.</returns>
    public static T GetHeaderOrDefault<T>(this Message message, string key, T defaultValue = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Header key cannot be null or whitespace.", nameof(key));

        if (message.Headers != null && message.Headers.TryGetValue(key, out var obj) && obj is T t)
            return t;

        return defaultValue;
    }

    /// <summary>
    /// Determines whether the message has expired based on its <see cref="Message.CreatedAt"/> timestamp
    /// and a supplied time‑to‑live.
    /// </summary>
    /// <param name="message">The message to evaluate.</param>
    /// <param name="ttl">Time‑to‑live duration.</param>
    /// <returns><c>true</c> if the message is older than <paramref name="ttl"/>; otherwise <c>false</c>.</returns>
    public static bool IsExpired(this Message message, TimeSpan ttl)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        return (DateTime.UtcNow - message.CreatedAt) > ttl;
    }
}
