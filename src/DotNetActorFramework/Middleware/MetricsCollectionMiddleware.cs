// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using System.Diagnostics;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that collects detailed metrics about message processing.
/// Tracks per-actor and per-message-type latency, throughput, and error rates
/// and stores them in the associated <see cref="MetricsCollector"/>.
/// </summary>
/// <remarks>
/// Runs at <c>Order = 200</c> so it executes last in the default pipeline and
/// captures the full end-to-end processing time after all other middleware.
/// </remarks>
public class MetricsCollectionMiddleware : IActorMiddleware
{
    /// <summary>
    /// Gets the display name of this middleware.
    /// </summary>
    public string Name => "MetricsCollectionMiddleware";

    /// <summary>
    /// Gets the execution order of this middleware in the pipeline. Higher values run later.
    /// </summary>
    public int Order => 200; // Run after all other middleware

    private readonly MetricsCollector _collector;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsCollectionMiddleware"/> class.
    /// </summary>
    /// <param name="collector">The collector that receives the recorded metrics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collector"/> is <c>null</c>.</exception>
    public MetricsCollectionMiddleware(MetricsCollector collector)
    {
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    /// <summary>
    /// Invokes the next middleware in the pipeline and records the processing time and outcome of the message.
    /// </summary>
    /// <param name="envelope">The envelope being processed.</param>
    /// <param name="next">The delegate that invokes the next stage of the pipeline.</param>
    /// <returns><c>true</c> if processing completed without an exception; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
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
                envelope.Recipient.Path.ToString(),
                message.GetType().Name,
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
    /// <summary>
    /// Gets or sets the name of the message type these metrics describe.
    /// </summary>
    public string MessageType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of messages of this type that were processed.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of messages of this type that failed processing.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the sum of processing times, in milliseconds, for all messages of this type.
    /// </summary>
    public long TotalLatencyMs { get; set; }

    /// <summary>
    /// Gets the average processing time per message, in milliseconds.
    /// </summary>
    /// <returns>The average latency, or <c>0</c> if no messages have been processed.</returns>
    public double GetAverageLatencyMs()
        => ProcessedCount > 0 ? (double)TotalLatencyMs / ProcessedCount : 0;

    /// <summary>
    /// Gets the percentage of processed messages of this type that failed.
    /// </summary>
    /// <returns>The error rate as a percentage from 0 to 100, or <c>0</c> if no messages have been processed.</returns>
    public double GetErrorRate()
        => ProcessedCount > 0 ? (double)ErrorCount / ProcessedCount * 100 : 0;
}

/// <summary>
/// Metrics for a specific actor.
/// </summary>
public class ActorMetrics
{
    /// <summary>
    /// Gets or sets the path of the actor these metrics describe.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of messages processed by this actor.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of messages this actor failed to process.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the sum of processing times, in milliseconds, for all messages processed by this actor.
    /// </summary>
    public long TotalLatencyMs { get; set; }

    /// <summary>
    /// Gets the average processing time per message, in milliseconds.
    /// </summary>
    /// <returns>The average latency, or <c>0</c> if no messages have been processed.</returns>
    public double GetAverageLatencyMs()
        => ProcessedCount > 0 ? (double)TotalLatencyMs / ProcessedCount : 0;

    /// <summary>
    /// Gets the percentage of messages processed by this actor that failed.
    /// </summary>
    /// <returns>The error rate as a percentage from 0 to 100, or <c>0</c> if no messages have been processed.</returns>
    public double GetErrorRate()
        => ProcessedCount > 0 ? (double)ErrorCount / ProcessedCount * 100 : 0;
}

/// <summary>
/// Overall system metrics summary.
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// Gets or sets the total number of messages processed across the system.
    /// </summary>
    public long TotalMessagesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages that failed processing across the system.
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets the average processing time per message across the system, in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the number of distinct message types that have been recorded.
    /// </summary>
    public int MessageTypeCount { get; set; }

    /// <summary>
    /// Gets or sets the number of distinct actors that have been recorded.
    /// </summary>
    public int ActorCount { get; set; }

    /// <summary>
    /// Gets the percentage of messages processed across the system that failed.
    /// </summary>
    /// <returns>The error rate as a percentage from 0 to 100, or <c>0</c> if no messages have been processed.</returns>
    public double GetErrorRate()
        => TotalMessagesProcessed > 0 ? (double)TotalErrors / TotalMessagesProcessed * 100 : 0;
}
