// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

using System.Threading;

/// <summary>
/// Tracks performance and behavior metrics for an actor.
/// </summary>
public class ActorMetrics
{
    public Guid ActorId { get; }
    public ActorPath ActorPath { get; }

    private long _messageCount;
    public long MessageCount => Interlocked.Read(ref _messageCount);

    private long _errorCount;
    public long ErrorCount => Interlocked.Read(ref _errorCount);

    private long _processedCount;
    public long ProcessedCount => Interlocked.Read(ref _processedCount);

    public double AverageProcessingTimeMs { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? LastMessageTime { get; private set; }

    /// <summary>
    /// Current number of messages waiting in this actor's mailbox.
    /// Updated by calling <see cref="UpdateMailboxDepth"/>.
    /// </summary>
    private int _mailboxDepth;
    public int MailboxDepth => _mailboxDepth;

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
        Interlocked.Increment(ref _messageCount);
        lock (_lockObject)
        {
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
            Interlocked.Increment(ref _processedCount);
            UpdateAverageProcessingTime();
        }
    }

    /// <summary>
    /// Records that an error occurred processing a message.
    /// </summary>
    public void RecordError()
    {
        Interlocked.Increment(ref _errorCount);
    }

    /// <summary>
    /// Updates the current mailbox depth snapshot.
    /// Should be called by the mailbox service whenever the queue depth changes,
    /// or at query time to reflect the live value.
    /// </summary>
    /// <param name="depth">The current number of messages waiting in the mailbox.</param>
    public void UpdateMailboxDepth(int depth)
    {
        Interlocked.Exchange(ref _mailboxDepth, depth);
    }

    /// <summary>
    /// Gets the error rate as a percentage.
    /// </summary>
    public double GetErrorRate()
    {
        var mc = MessageCount;
        if (mc == 0) return 0;
        return (double)ErrorCount / mc * 100;
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
                IsHealthy = !IsUnhealthy(),
                MailboxDepth = MailboxDepth
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

    public override string ToString() => $"ActorMetrics {{ ActorPath = {ActorPath}, MessageCount = {MessageCount}, ProcessedCount = {ProcessedCount}, ErrorCount = {ErrorCount}, ErrorRate = {GetErrorRate()}, SuccessRate = {GetSuccessRate()} }}";
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
    /// <summary>
    /// Current number of messages waiting in this actor's mailbox at the time of the snapshot.
    /// </summary>
    public int MailboxDepth { get; set; }
}
