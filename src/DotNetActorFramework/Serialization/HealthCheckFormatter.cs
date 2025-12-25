// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Serialization;

/// <summary>
/// Formatter for system health checks and diagnostics.
/// Provides formatted output of actor system health information.
/// </summary>
public interface IHealthCheckFormatter
{
    /// <summary>
    /// Formats health check information into a string.
    /// </summary>
    string Format(SystemHealthSummary health);

    /// <summary>
    /// Gets the content type for the formatted output.
    /// </summary>
    string ContentType { get; }
}

/// <summary>
/// JSON health check formatter.
/// </summary>
public class JsonHealthCheckFormatter : IHealthCheckFormatter
{
    public string ContentType => "application/json";

    public string Format(SystemHealthSummary health)
    {
        if (health == null) return "null";
        return health.ToJsonPretty();
    }
}

/// <summary>
/// Human-readable health check formatter.
/// </summary>
public class TextHealthCheckFormatter : IHealthCheckFormatter
{
    public string ContentType => "text/plain";

    public string Format(SystemHealthSummary health)
    {
        if (health == null) return "No health data";

        var sb = new StringBuilder();
        sb.AppendLine("=== Actor System Health ===");
        sb.AppendLine($"System:        {health.SystemName} ({health.SystemId:N})");
        sb.AppendLine($"Created:       {health.CreatedAt:O}");
        sb.AppendLine($"Total Actors:  {health.TotalActors}");
        sb.AppendLine($"Healthy:       {health.HealthyActors} ({GetPercentage(health.HealthyActors, health.TotalActors)}%)");
        sb.AppendLine($"Unhealthy:     {health.UnhealthyActors} ({GetPercentage(health.UnhealthyActors, health.TotalActors)}%)");
        sb.AppendLine($"Error State:   {health.ErrorActors}");
        sb.AppendLine($"Total Messages: {health.TotalMessages}");
        sb.AppendLine($"Total Errors:  {health.TotalErrors} ({health.GetErrorRate():F2}%)");
        sb.AppendLine($"Status:        {(health.IsHealthy ? "HEALTHY" : "UNHEALTHY")}");

        return sb.ToString();
    }

    private static double GetPercentage(int part, int total)
    {
        return total > 0 ? (double)part / total * 100 : 0;
    }
}

/// <summary>
/// CSV health check formatter for exporting metrics.
/// </summary>
public class CsvHealthCheckFormatter : IHealthCheckFormatter
{
    public string ContentType => "text/csv";

    public string Format(SystemHealthSummary health)
    {
        if (health == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("SystemId,SystemName,CreatedAt,TotalActors,HealthyActors,UnhealthyActors,ErrorActors,TotalMessages,TotalErrors,ErrorRate,HealthPercentage");

        var errorRate = health.GetErrorRate();
        var healthPct = health.GetHealthPercentage();

        sb.AppendLine(
            $"\"{health.SystemId:N}\",\"{health.SystemName}\",\"{health.CreatedAt:O}\"," +
            $"{health.TotalActors},{health.HealthyActors},{health.UnhealthyActors},{health.ErrorActors}," +
            $"{health.TotalMessages},{health.TotalErrors},{errorRate:F2},{healthPct:F2}");

        return sb.ToString();
    }
}

/// <summary>
/// Factory for creating health check formatters.
/// </summary>
public class HealthCheckFormatterFactory
{
    private readonly Dictionary<string, IHealthCheckFormatter> _formatters = [];

    public HealthCheckFormatterFactory()
    {
        _formatters["json"] = new JsonHealthCheckFormatter();
        _formatters["text"] = new TextHealthCheckFormatter();
        _formatters["csv"] = new CsvHealthCheckFormatter();
        _formatters["application/json"] = new JsonHealthCheckFormatter();
        _formatters["text/plain"] = new TextHealthCheckFormatter();
        _formatters["text/csv"] = new CsvHealthCheckFormatter();
    }

    /// <summary>
    /// Registers a custom formatter.
    /// </summary>
    public void Register(string key, IHealthCheckFormatter formatter)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty.", nameof(key));

        _formatters[key.ToLowerInvariant()] = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Gets a formatter by name or content type.
    /// </summary>
    public IHealthCheckFormatter? GetFormatter(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _formatters.TryGetValue(key.ToLowerInvariant(), out var formatter);
        return formatter;
    }

    /// <summary>
    /// Formats health check using the specified formatter.
    /// </summary>
    public string Format(SystemHealthSummary health, string formatterKey)
    {
        var formatter = GetFormatter(formatterKey) ?? new JsonHealthCheckFormatter();
        return formatter.Format(health);
    }
}
