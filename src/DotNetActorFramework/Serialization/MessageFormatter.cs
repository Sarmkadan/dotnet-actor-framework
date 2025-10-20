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
/// Supports multiple output formats for different use cases (logs, APIs, exports).
/// </summary>
public interface IMessageFormatter
{
    /// <summary>
    /// Formats a message into a string representation.
    /// </summary>
    string Format(Message message);

    /// <summary>
    /// Gets the content type for the formatted output.
    /// </summary>
    string ContentType { get; }
}

/// <summary>
/// JSON formatter for messages.
/// </summary>
public class JsonMessageFormatter : IMessageFormatter
{
    public string ContentType => "application/json";

    public string Format(Message message)
    {
        if (message == null)
            return "null";

        return message.ToJsonPretty();
    }
}

/// <summary>
/// Human-readable text formatter for messages.
/// Produces formatted output suitable for logs and displays.
/// </summary>
public class TextMessageFormatter : IMessageFormatter
{
    public string ContentType => "text/plain";

    public string Format(Message message)
    {
        if (message == null)
            return "null";

        var sb = new StringBuilder();
        sb.AppendLine("=== Message ===");
        sb.AppendLine($"ID:        {message.Id:N}");
        sb.AppendLine($"Type:      {message.Type}");
        sb.AppendLine($"Priority:  {message.Priority}");
        sb.AppendLine($"Created:   {message.CreatedAt:O}");
        sb.AppendLine($"Age:       {message.GetAge()}ms");
        sb.AppendLine($"Payload:   {FormatPayload(message.Payload)}");

        if (message.Metadata != null && message.Metadata.Count > 0)
        {
            sb.AppendLine("Metadata:");
            foreach (var kvp in message.Metadata)
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }

        return sb.ToString();
    }

    private static string FormatPayload(object? payload)
    {
        if (payload == null) return "null";
        if (payload is string str) return str;
        try { return payload.ToJson(); }
        catch { return payload.ToString() ?? "unknown"; }
    }
}

/// <summary>
/// CSV formatter for exporting message information.
/// </summary>
public class CsvMessageFormatter : IMessageFormatter
{
    public string ContentType => "text/csv";
    public bool IncludeHeaders { get; set; } = true;

    public string Format(Message message)
    {
        if (message == null)
            return string.Empty;

        var sb = new StringBuilder();

        if (IncludeHeaders)
        {
            sb.AppendLine("MessageId,Type,Priority,CreatedAt,Age,PayloadLength");
        }

        var payloadStr = message.Payload?.ToString() ?? "";
        sb.AppendLine(
            $"\"{message.Id:N}\",\"{message.Type}\",\"{message.Priority}\"," +
            $"\"{message.CreatedAt:O}\",{message.GetAge()},{payloadStr.Length}");

        return sb.ToString();
    }
}

/// <summary>
/// Formatter factory for creating formatters by type.
/// </summary>
public class MessageFormatterFactory
{
    private readonly Dictionary<string, IMessageFormatter> _formatters = [];

    public MessageFormatterFactory()
    {
        // Register default formatters
        _formatters["json"] = new JsonMessageFormatter();
        _formatters["text"] = new TextMessageFormatter();
        _formatters["csv"] = new CsvMessageFormatter();
        _formatters["application/json"] = new JsonMessageFormatter();
        _formatters["text/plain"] = new TextMessageFormatter();
        _formatters["text/csv"] = new CsvMessageFormatter();
    }

    /// <summary>
    /// Registers a custom formatter.
    /// </summary>
    public void Register(string key, IMessageFormatter formatter)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty.", nameof(key));

        _formatters[key.ToLowerInvariant()] = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Gets a formatter by name or content type.
    /// </summary>
    public IMessageFormatter? GetFormatter(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _formatters.TryGetValue(key.ToLowerInvariant(), out var formatter);
        return formatter;
    }

    /// <summary>
    /// Formats a message using the specified formatter.
    /// </summary>
    public string Format(Message message, string formatterKey)
    {
        var formatter = GetFormatter(formatterKey) ?? new JsonMessageFormatter();
        return formatter.Format(message);
    }
}
