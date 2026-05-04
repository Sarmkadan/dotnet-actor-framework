// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

/// <summary>
/// Tracks performance and behavior metrics for an actor.
/// </summary>
public class ActorMetrics
{
    public Guid ActorId { get; }
    public ActorPath ActorPath { get; }
    public long MessageCount { get; private set; }
    public long ErrorCount { get; private set; }
    public long ProcessedCount { get; private set; }
    public double AverageProcessingTimeMs { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? LastMessageTime { get; private set; }

    private readonly List<long> _processingTimes = [];
    private readonly object _lockObject = new();

    public ActorMetrics(Guid actorId, ActorPath actorPath)
    {
        ActorId = actorId;
        ActorPath = actorPath ?? throw new ArgumentNullException(nameof(actorPath));
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that a message was received.
    /// </summary>
    public void RecordMessageReceived()
    {
        lock (_lockObject)
        {
            MessageCount++;
            LastMessageTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Records the processing time of a message in milliseconds.
    /// </summary>
    public void RecordProcessingTime(long elapsedMilliseconds)
    {
        lock (_lockObject)
        {
            _processingTimes.Add(elapsedMilliseconds);
            ProcessedCount++;
            UpdateAverageProcessingTime();
        }
    }

    /// <summary>
    /// Records that an error occurred processing a message.
    /// </summary>
    public void RecordError()
    {
        lock (_lockObject)
        {
            ErrorCount++;
        }
    }

    /// <summary>
    /// Gets the error rate as a percentage.
    /// </summary>
    public double GetErrorRate()
    {
        lock (_lockObject)
        {
            if (MessageCount == 0) return 0;
            return (double)ErrorCount / MessageCount * 100;
        }
    }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double GetSuccessRate() => 100 - GetErrorRate();

    /// <summary>
    /// Gets the total uptime since creation.
    /// </summary>
    public TimeSpan GetUptime() => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Checks if the actor is experiencing high error rates.
    /// </summary>
    public bool IsUnhealthy(double errorRateThreshold = 0.25) => GetErrorRate() > errorRateThreshold;

    /// <summary>
    /// Gets a summary of the metrics.
    /// </summary>
    public ActorMetricsSummary GetSummary()
    {
        lock (_lockObject)
        {
            return new ActorMetricsSummary
            {
                ActorPath = ActorPath,
                MessageCount = MessageCount,
                ProcessedCount = ProcessedCount,
                ErrorCount = ErrorCount,
                ErrorRate = GetErrorRate(),
                SuccessRate = GetSuccessRate(),
                AverageProcessingTimeMs = AverageProcessingTimeMs,
                Uptime = GetUptime(),
                IsHealthy = !IsUnhealthy()
            };
        }
    }

    private void UpdateAverageProcessingTime()
    {
        if (_processingTimes.Count == 0)
        {
            AverageProcessingTimeMs = 0;
            return;
        }

        AverageProcessingTimeMs = _processingTimes.Average();
    }
}

/// <summary>
/// Summary snapshot of actor metrics.
/// </summary>
public class ActorMetricsSummary
{
    public ActorPath ActorPath { get; set; }
    public long MessageCount { get; set; }
    public long ProcessedCount { get; set; }
    public long ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double SuccessRate { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public TimeSpan Uptime { get; set; }
    public bool IsHealthy { get; set; }
}
