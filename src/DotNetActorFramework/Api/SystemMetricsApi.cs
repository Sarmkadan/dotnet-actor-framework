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

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemMetricsApi"/> class.
    /// </summary>
    /// <param name="actorSystem">The actor system to report health for.</param>
    /// <param name="metricsCollector">The collector that supplies performance metrics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorSystem"/> or <paramref name="metricsCollector"/> is null.</exception>
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
    /// <summary>
    /// Gets or sets the name of the actor system.
    /// </summary>
    public string SystemName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the actor system.
    /// </summary>
    public Guid SystemId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the system is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the total number of actors in the system.
    /// </summary>
    public int TotalActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors in a healthy state.
    /// </summary>
    public int HealthyActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors in an unhealthy state.
    /// </summary>
    public int UnhealthyActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors that are in an error state.
    /// </summary>
    public int ErrorActors { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages processed by the system.
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the total number of errors recorded by the system.
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets the ratio of errors to processed messages.
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the percentage of actors that are healthy.
    /// </summary>
    public double HealthPercentage { get; set; }

    /// <summary>
    /// Gets or sets the average message processing latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the summary was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Metrics for a message type.
/// </summary>
public class MessageTypeMetricsInfo
{
    /// <summary>
    /// Gets or sets the name of the message type.
    /// </summary>
    public string MessageType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of messages of this type that were processed.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of errors raised while processing this message type.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the average processing latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the ratio of errors to processed messages for this message type.
    /// </summary>
    public double ErrorRate { get; set; }
}

/// <summary>
/// Metrics for an actor.
/// </summary>
public class ActorMetricsInfo
{
    /// <summary>
    /// Gets or sets the path of the actor.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of messages processed by the actor.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of errors raised by the actor.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the average message processing latency of the actor in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the ratio of errors to processed messages for the actor.
    /// </summary>
    public double ErrorRate { get; set; }
}
