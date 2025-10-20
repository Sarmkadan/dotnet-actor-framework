// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using System.Diagnostics;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that collects detailed metrics about message processing.
/// Tracks latency, throughput, error rates, and other performance indicators.
/// </summary>
public class MetricsCollectionMiddleware : IActorMiddleware
{
    public string Name => "MetricsCollectionMiddleware";
    public int Order => 200; // Run after all other middleware

    private readonly MetricsCollector _collector;

    public MetricsCollectionMiddleware(MetricsCollector collector)
    {
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    public async Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        var message = envelope.Message;
        var stopwatch = Stopwatch.StartNew();
        var success = true;

        try
        {
            await next(envelope);
            return true;
        }
        catch
        {
            success = false;
            return false;
        }
        finally
        {
            stopwatch.Stop();
            _collector.RecordMessageProcessed(
                envelope.RecipientPath.ToString(),
                message.Type,
                stopwatch.ElapsedMilliseconds,
                success);
        }
    }
}

/// <summary>
/// Collects and stores metrics about message processing.
/// Useful for monitoring system health and identifying performance bottlenecks.
/// </summary>
public class MetricsCollector
{
    private readonly Dictionary<string, MessageTypeMetrics> _messageMetrics = [];
    private readonly Dictionary<string, ActorMetrics> _actorMetrics = [];
    private readonly object _lock = new();

    /// <summary>
    /// Records that a message was processed.
    /// </summary>
    public void RecordMessageProcessed(string actorPath, string messageType, long elapsedMs, bool success)
    {
        lock (_lock)
        {
            // Record by message type
            if (!_messageMetrics.TryGetValue(messageType, out var msgMetrics))
            {
                msgMetrics = new MessageTypeMetrics { MessageType = messageType };
                _messageMetrics[messageType] = msgMetrics;
            }
            msgMetrics.ProcessedCount++;
            msgMetrics.TotalLatencyMs += elapsedMs;
            if (!success) msgMetrics.ErrorCount++;

            // Record by actor
            if (!_actorMetrics.TryGetValue(actorPath, out var actorMetrics))
            {
                actorMetrics = new ActorMetrics { ActorPath = actorPath };
                _actorMetrics[actorPath] = actorMetrics;
            }
            actorMetrics.ProcessedCount++;
            actorMetrics.TotalLatencyMs += elapsedMs;
            if (!success) actorMetrics.ErrorCount++;
        }
    }

    /// <summary>
    /// Gets metrics for a specific message type.
    /// </summary>
    public MessageTypeMetrics? GetMessageTypeMetrics(string messageType)
    {
        lock (_lock)
        {
            return _messageMetrics.TryGetValue(messageType, out var metrics) ? metrics : null;
        }
    }

    /// <summary>
    /// Gets metrics for a specific actor.
    /// </summary>
    public ActorMetrics? GetActorMetrics(string actorPath)
    {
        lock (_lock)
        {
            return _actorMetrics.TryGetValue(actorPath, out var metrics) ? metrics : null;
        }
    }

    /// <summary>
    /// Gets all message type metrics.
    /// </summary>
    public IReadOnlyList<MessageTypeMetrics> GetAllMessageMetrics()
    {
        lock (_lock)
        {
            return _messageMetrics.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all actor metrics.
    /// </summary>
    public IReadOnlyList<ActorMetrics> GetAllActorMetrics()
    {
        lock (_lock)
        {
            return _actorMetrics.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets overall system metrics.
    /// </summary>
    public SystemMetrics GetSystemMetrics()
    {
        lock (_lock)
        {
            var totalProcessed = _messageMetrics.Values.Sum(m => m.ProcessedCount);
            var totalErrors = _messageMetrics.Values.Sum(m => m.ErrorCount);
            var totalLatency = _messageMetrics.Values.Sum(m => m.TotalLatencyMs);

            return new SystemMetrics
            {
                TotalMessagesProcessed = totalProcessed,
                TotalErrors = totalErrors,
                AverageLatencyMs = totalProcessed > 0 ? (double)totalLatency / totalProcessed : 0,
                MessageTypeCount = _messageMetrics.Count,
                ActorCount = _actorMetrics.Count
            };
        }
    }

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _messageMetrics.Clear();
            _actorMetrics.Clear();
        }
    }
}

/// <summary>
/// Metrics for a specific message type.
/// </summary>
public class MessageTypeMetrics
{
    public string MessageType { get; set; }
    public long ProcessedCount { get; set; }
    public long ErrorCount { get; set; }
    public long TotalLatencyMs { get; set; }

    public double GetAverageLatencyMs()
        => ProcessedCount > 0 ? (double)TotalLatencyMs / ProcessedCount : 0;

    public double GetErrorRate()
        => ProcessedCount > 0 ? (double)ErrorCount / ProcessedCount * 100 : 0;
}

/// <summary>
/// Metrics for a specific actor.
/// </summary>
public class ActorMetrics
{
    public string ActorPath { get; set; }
    public long ProcessedCount { get; set; }
    public long ErrorCount { get; set; }
    public long TotalLatencyMs { get; set; }

    public double GetAverageLatencyMs()
        => ProcessedCount > 0 ? (double)TotalLatencyMs / ProcessedCount : 0;

    public double GetErrorRate()
        => ProcessedCount > 0 ? (double)ErrorCount / ProcessedCount * 100 : 0;
}

/// <summary>
/// Overall system metrics summary.
/// </summary>
public class SystemMetrics
{
    public long TotalMessagesProcessed { get; set; }
    public long TotalErrors { get; set; }
    public double AverageLatencyMs { get; set; }
    public int MessageTypeCount { get; set; }
    public int ActorCount { get; set; }

    public double GetErrorRate()
        => TotalMessagesProcessed > 0 ? (double)TotalErrors / TotalMessagesProcessed * 100 : 0;
}
