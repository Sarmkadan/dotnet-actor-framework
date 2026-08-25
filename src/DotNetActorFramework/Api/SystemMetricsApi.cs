// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Middleware;

namespace DotNetActorFramework.Api;

/// <summary>
/// API handler for system metrics and monitoring.
/// Provides access to performance metrics and health information.
/// </summary>
public class SystemMetricsApi
{
    private readonly ActorSystem _actorSystem;
    private readonly MetricsCollector _metricsCollector;

    public SystemMetricsApi(ActorSystem actorSystem, MetricsCollector metricsCollector)
    {
        _actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }

    /// <summary>
    /// Gets the overall system health summary.
    /// </summary>
    public HealthSummary GetSystemHealth()
    {
        var health = _actorSystem.GetHealthSummary();
        var metrics = _metricsCollector.GetSystemMetrics();

        return new HealthSummary
        {
            SystemName = health.SystemName,
            SystemId = health.SystemId,
            IsHealthy = health.IsHealthy,
            TotalActors = health.TotalActors,
            HealthyActors = health.HealthyActors,
            UnhealthyActors = health.UnhealthyActors,
            ErrorActors = health.ErrorActors,
            TotalMessages = health.TotalMessages,
            TotalErrors = health.TotalErrors,
            ErrorRate = health.GetErrorRate(),
            HealthPercentage = health.GetHealthPercentage(),
            AverageLatencyMs = metrics.AverageLatencyMs,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets metrics for a specific message type.
    /// </summary>
    public MessageTypeMetricsInfo? GetMessageTypeMetrics(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            return null;

        var metrics = _metricsCollector.GetMessageTypeMetrics(messageType);
        if (metrics == null)
            return null;

        return new MessageTypeMetricsInfo
        {
            MessageType = messageType,
            ProcessedCount = metrics.ProcessedCount,
            ErrorCount = metrics.ErrorCount,
            AverageLatencyMs = metrics.GetAverageLatencyMs(),
            ErrorRate = metrics.GetErrorRate()
        };
    }

    /// <summary>
    /// Gets metrics for a specific actor.
    /// </summary>
    public ActorMetricsInfo? GetActorMetrics(string actorPath)
    {
        if (string.IsNullOrWhiteSpace(actorPath))
            return null;

        var metrics = _metricsCollector.GetActorMetrics(actorPath);
        if (metrics == null)
            return null;

        return new ActorMetricsInfo
        {
            ActorPath = actorPath,
            ProcessedCount = metrics.ProcessedCount,
            ErrorCount = metrics.ErrorCount,
            AverageLatencyMs = metrics.GetAverageLatencyMs(),
            ErrorRate = metrics.GetErrorRate()
        };
    }

    /// <summary>
    /// Gets top message types by count.
    /// </summary>
    public List<MessageTypeMetricsInfo> GetTopMessageTypes(int limit = 10)
    {
        var allMetrics = _metricsCollector.GetAllMessageMetrics();

        return allMetrics
            .OrderByDescending(m => m.ProcessedCount)
            .Take(limit)
            .Select(m => new MessageTypeMetricsInfo
            {
                MessageType = m.MessageType,
                ProcessedCount = m.ProcessedCount,
                ErrorCount = m.ErrorCount,
                AverageLatencyMs = m.GetAverageLatencyMs(),
                ErrorRate = m.GetErrorRate()
            })
            .ToList();
    }

    /// <summary>
    /// Gets slowest actors by average latency.
    /// </summary>
    public List<ActorMetricsInfo> GetSlowesttActors(int limit = 10)
    {
        var allMetrics = _metricsCollector.GetAllActorMetrics();

        return allMetrics
            .OrderByDescending(m => m.GetAverageLatencyMs())
            .Take(limit)
            .Select(m => new ActorMetricsInfo
            {
                ActorPath = m.ActorPath,
                ProcessedCount = m.ProcessedCount,
                ErrorCount = m.ErrorCount,
                AverageLatencyMs = m.GetAverageLatencyMs(),
                ErrorRate = m.GetErrorRate()
            })
            .ToList();
    }

    /// <summary>
    /// Gets most error-prone actors.
    /// </summary>
    public List<ActorMetricsInfo> GetErrorProneActors(int limit = 10)
    {
        var allMetrics = _metricsCollector.GetAllActorMetrics();

        return allMetrics
            .Where(m => m.ErrorCount > 0)
            .OrderByDescending(m => m.GetErrorRate())
            .Take(limit)
            .Select(m => new ActorMetricsInfo
            {
                ActorPath = m.ActorPath,
                ProcessedCount = m.ProcessedCount,
                ErrorCount = m.ErrorCount,
                AverageLatencyMs = m.GetAverageLatencyMs(),
                ErrorRate = m.GetErrorRate()
            })
            .ToList();
    }

    /// <summary>
    /// Resets all metrics (useful for benchmarking).
    /// </summary>
    public void ResetMetrics()
    {
        _metricsCollector.Reset();
    }

    /// <summary>
    /// Returns a concise, informative representation of the current system state.
    /// </summary>
    public override string ToString()
    {
        var health = GetSystemHealth();
        return $"SystemMetricsApi {{ SystemName = {health.SystemName}, SystemId = {health.SystemId}, IsHealthy = {health.IsHealthy}, TotalActors = {health.TotalActors}, HealthyActors = {health.HealthyActors}, UnhealthyActors = {health.UnhealthyActors} }}";
    }
}

/// <summary>
/// System health summary.
/// </summary>
public class HealthSummary
{
    public string SystemName { get; set; }
    public Guid SystemId { get; set; }
    public bool IsHealthy { get; set; }
    public int TotalActors { get; set; }
    public int HealthyActors { get; set; }
    public int UnhealthyActors { get; set; }
    public int ErrorActors { get; set; }
    public long TotalMessages { get; set; }
    public long TotalErrors { get; set; }
    public double ErrorRate { get; set; }
    public double HealthPercentage { get; set; }
    public double AverageLatencyMs { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Metrics for a message type.
/// </summary>
public class MessageTypeMetricsInfo
{
    public string MessageType { get; set; }
    public long ProcessedCount { get; set; }
    public long ErrorCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double ErrorRate { get; set; }
}

/// <summary>
/// Metrics for an actor.
/// </summary>
public class ActorMetricsInfo
{
    public string ActorPath { get; set; }
    public long ProcessedCount { get; set; }
    public long ErrorCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double ErrorRate { get; set; }
}
