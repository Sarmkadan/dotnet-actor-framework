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
    /// <summary>
    /// Gets the unique identifier of the actor these metrics belong to.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// Gets the path of the actor these metrics belong to.
    /// </summary>
    public ActorPath ActorPath { get; }

    private long _messageCount;

    /// <summary>
    /// Gets the total number of messages received by the actor.
    /// </summary>
    public long MessageCount => Interlocked.Read(ref _messageCount);

    private long _errorCount;

    /// <summary>
    /// Gets the total number of errors recorded while processing messages.
    /// </summary>
    public long ErrorCount => Interlocked.Read(ref _errorCount);

    private long _processedCount;

    /// <summary>
    /// Gets the total number of messages whose processing time has been recorded.
    /// </summary>
    public long ProcessedCount => Interlocked.Read(ref _processedCount);

    /// <summary>
    /// Gets the average message processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; private set; }

    /// <summary>
    /// Gets the UTC time at which the metrics were created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the UTC time at which the most recent message was received, or <c>null</c> if none has been received.
    /// </summary>
    public DateTime? LastMessageTime { get; private set; }

    /// <summary>
    /// Current number of messages waiting in this actor's mailbox.
    /// Updated by calling <see cref="UpdateMailboxDepth"/>.
    /// </summary>
    private int _mailboxDepth;

    /// <summary>
    /// Gets the last recorded number of messages waiting in this actor's mailbox.
    /// </summary>
    public int MailboxDepth => _mailboxDepth;

    private readonly List<long> _processingTimes = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorMetrics"/> class.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorPath"/> is <c>null</c>.</exception>
    public ActorMetrics(Guid actorId, ActorPath actorPath)
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ActorId = actorId;
        ActorPath = actorPath;
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

    /// <summary>
    /// Returns a string representation of the current metrics.
    /// </summary>
    /// <returns>A string containing the actor path and the main counters and rates.</returns>
    public override string ToString() => $"ActorMetrics {{ ActorPath = {ActorPath}, MessageCount = {MessageCount}, ProcessedCount = {ProcessedCount}, ErrorCount = {ErrorCount}, ErrorRate = {GetErrorRate()}, SuccessRate = {GetSuccessRate()} }}";
}

/// <summary>
/// Summary snapshot of actor metrics.
/// </summary>
public class ActorMetricsSummary
{
    /// <summary>
    /// Gets or sets the path of the actor the snapshot was taken from.
    /// </summary>
    public ActorPath ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total number of messages received by the actor.
    /// </summary>
    public long MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages whose processing time has been recorded.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of errors recorded while processing messages.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the error rate as a percentage.
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the success rate as a percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average message processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the time elapsed since the actor's metrics were created.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the actor's error rate is within the healthy threshold.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Current number of messages waiting in this actor's mailbox at the time of the snapshot.
    /// </summary>
    public int MailboxDepth { get; set; }
}
