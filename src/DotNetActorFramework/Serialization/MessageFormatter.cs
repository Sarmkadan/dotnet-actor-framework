// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Serialization;

/// <summary>
/// Interface for formatting messages into different output formats.
/// </summary>
public interface IMessageFormatter
{
    /// <summary>
    /// Formats the specified message into a string representation.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The formatted message.</returns>
    string Format(Message message);

    /// <summary>
    /// Gets the MIME content type produced by this formatter.
    /// </summary>
    string ContentType { get; }
}

/// <summary>
/// JSON formatter for messages.
/// </summary>
public class JsonMessageFormatter : IMessageFormatter
{
    /// <summary>
    /// Gets the MIME content type for JSON output (<c>application/json</c>).
    /// </summary>
    public string ContentType => "application/json";

    /// <summary>
    /// Formats the message as pretty-printed JSON.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The JSON representation, or <c>"null"</c> if the message is <c>null</c>.</returns>
    public string Format(Message message)
    {
        if (message == null) return "null";
        return message.ToJsonPretty();
    }
}

/// <summary>
/// Human-readable text formatter for messages.
/// </summary>
public class TextMessageFormatter : IMessageFormatter
{
    /// <summary>
    /// Gets the MIME content type for plain text output (<c>text/plain</c>).
    /// </summary>
    public string ContentType => "text/plain";

    /// <summary>
    /// Formats the message as a human-readable text block with its ID, type, priority, creation time and age.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The text representation, or <c>"null"</c> if the message is <c>null</c>.</returns>
    public string Format(Message message)
    {
        if (message == null) return "null";

        var sb = new StringBuilder();
        sb.AppendLine("=== Message ===");
        sb.AppendLine($"ID:        {message.MessageId:N}");
        sb.AppendLine($"Type:      {message.GetType().Name}");
        sb.AppendLine($"Priority:  {message.Priority}");
        sb.AppendLine($"Created:   {message.CreatedAt:O}");
        sb.AppendLine($"Age:       {message.GetAge()}ms");
        return sb.ToString();
    }
}

/// <summary>
/// CSV formatter for exporting message information.
/// </summary>
public class CsvMessageFormatter : IMessageFormatter
{
    /// <summary>
    /// Gets the MIME content type for CSV output (<c>text/csv</c>).
    /// </summary>
    public string ContentType => "text/csv";

    /// <summary>
    /// Gets or sets a value indicating whether a header row is written before the data row. Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Formats the message as a CSV row, optionally preceded by a header row.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The CSV representation, or an empty string if the message is <c>null</c>.</returns>
    public string Format(Message message)
    {
        if (message == null) return string.Empty;

        var sb = new StringBuilder();
        if (IncludeHeaders)
            sb.AppendLine("MessageId,Type,Priority,CreatedAt,Age");

        sb.AppendLine(
            $"\"{message.MessageId:N}\",\"{message.GetType().Name}\",\"{message.Priority}\"," +
            $"\"{message.CreatedAt:O}\",{message.GetAge()}");

        return sb.ToString();
    }
}

/// <summary>
/// Factory for creating message formatters by type or content type key.
/// </summary>
public class MessageFormatterFactory
{
    private readonly Dictionary<string, IMessageFormatter> _formatters = [];

    /// <summary>
    /// Initializes a new instance with the built-in JSON, text and CSV formatters registered under
    /// short keys (<c>json</c>, <c>text</c>, <c>csv</c>) and MIME content types.
    /// </summary>
    public MessageFormatterFactory()
    {
        _formatters["json"] = new JsonMessageFormatter();
        _formatters["text"] = new TextMessageFormatter();
        _formatters["csv"] = new CsvMessageFormatter();
        _formatters["application/json"] = new JsonMessageFormatter();
        _formatters["text/plain"] = new TextMessageFormatter();
        _formatters["text/csv"] = new CsvMessageFormatter();
    }

    /// <summary>
    /// Registers a formatter under the specified key, replacing any formatter previously registered under it.
    /// Keys are matched case-insensitively.
    /// </summary>
    /// <param name="key">The key used to look up the formatter. Must not be null, empty or whitespace.</param>
    /// <param name="formatter">The formatter to register. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <c>null</c>.</exception>
    public void Register(string key, IMessageFormatter formatter)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty.", nameof(key));

        _formatters[key.ToLowerInvariant()] = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Gets the formatter registered under the specified key, matched case-insensitively.
    /// </summary>
    /// <param name="key">The formatter key or content type.</param>
    /// <returns>The matching formatter, or <c>null</c> if the key is null, empty or not registered.</returns>
    public IMessageFormatter? GetFormatter(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _formatters.TryGetValue(key.ToLowerInvariant(), out var formatter);
        return formatter;
    }

    /// <summary>
    /// Formats the message using the formatter registered under the specified key.
    /// Falls back to the JSON formatter when no formatter is registered under that key.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <param name="formatterKey">The formatter key or content type.</param>
    /// <returns>The formatted message.</returns>
    public string Format(Message message, string formatterKey)
    {
        var formatter = GetFormatter(formatterKey) ?? new JsonMessageFormatter();
        return formatter.Format(message);
    }
}
