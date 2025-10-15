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
    string Format(Message message);
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
        if (message == null) return "null";
        return message.ToJsonPretty();
    }
}

/// <summary>
/// Human-readable text formatter for messages.
/// </summary>
public class TextMessageFormatter : IMessageFormatter
{
    public string ContentType => "text/plain";

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
    public string ContentType => "text/csv";
    public bool IncludeHeaders { get; set; } = true;

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

    public MessageFormatterFactory()
    {
        _formatters["json"] = new JsonMessageFormatter();
        _formatters["text"] = new TextMessageFormatter();
        _formatters["csv"] = new CsvMessageFormatter();
        _formatters["application/json"] = new JsonMessageFormatter();
        _formatters["text/plain"] = new TextMessageFormatter();
        _formatters["text/csv"] = new CsvMessageFormatter();
    }

    public void Register(string key, IMessageFormatter formatter)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty.", nameof(key));

        _formatters[key.ToLowerInvariant()] = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    public IMessageFormatter? GetFormatter(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _formatters.TryGetValue(key.ToLowerInvariant(), out var formatter);
        return formatter;
    }

    public string Format(Message message, string formatterKey)
    {
        var formatter = GetFormatter(formatterKey) ?? new JsonMessageFormatter();
        return formatter.Format(message);
    }
}
