// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Middleware;
using DotNetActorFramework.Options;

namespace DotNetActorFramework.BackgroundWorkers;

/// <summary>
/// Background worker that periodically collects and aggregates metrics from the actor system.
/// Provides health monitoring and performance analysis capabilities.
/// </summary>
public class MetricsCollectorWorker : IBackgroundWorker
{
    /// <summary>
    /// Gets the unique identifier of this worker.
    /// </summary>
    public string WorkerId => "metrics-collector";

    /// <summary>
    /// Gets or sets the interval between metric collection runs.
    /// </summary>
    // The interval is now driven by configuration via MetricsCollectorOptions.
    public TimeSpan Interval { get; set; }

    private readonly ActorSystem _actorSystem;
    private readonly MetricsCollector _metricsCollector;
    private readonly MetricsCollectorOptions _options;
    private readonly MetricsSnapshot _latestSnapshot = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsCollectorWorker"/> class.
    /// </summary>
    /// <param name="actorSystem">The actor system whose health is monitored.</param>
    /// <param name="metricsCollector">The collector that provides system-level metrics.</param>
    /// <param name="options">Optional worker options; defaults are used when <c>null</c>.</param>
    public MetricsCollectorWorker(
        ActorSystem actorSystem,
        MetricsCollector metricsCollector,
        MetricsCollectorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(actorSystem);
        ArgumentNullException.ThrowIfNull(metricsCollector);

        _actorSystem = actorSystem;
        _metricsCollector = metricsCollector;

        // If no options are supplied, fall back to defaults.
        _options = options ?? new MetricsCollectorOptions();

        // Apply the configured interval.
        Interval = _options.Interval;
    }

    /// <summary>
    /// Collects the current system metrics and stores them in the latest snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the collection run.</param>
    /// <returns>A task that completes when the metrics have been collected.</returns>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var systemMetrics = _metricsCollector.GetSystemMetrics();
            var health = _actorSystem.GetHealthSummary();

            _latestSnapshot.Timestamp = DateTime.UtcNow;
            _latestSnapshot.TotalActors = health.TotalActors;
            _latestSnapshot.HealthyActors = health.HealthyActors;
            _latestSnapshot.ErrorActors = health.ErrorActors;
            _latestSnapshot.TotalMessages = health.TotalMessages;
            _latestSnapshot.TotalErrors = health.TotalErrors;
            _latestSnapshot.AverageLatencyMs = systemMetrics.AverageLatencyMs;
            _latestSnapshot.ErrorRate = systemMetrics.GetErrorRate();

            // Apply the configured error‑rate threshold to the snapshot.
            _latestSnapshot.ErrorRateThreshold = _options.ErrorRateThreshold;

        }, cancellationToken);
    }

    /// <summary>
    /// Gets the latest collected metrics snapshot.
    /// </summary>
    public MetricsSnapshot GetLatestSnapshot() => _latestSnapshot;

    /// <summary>
    /// Returns a string representation of the worker and its latest snapshot values.
    /// </summary>
    /// <returns>A human-readable summary of the worker state.</returns>
    public override string ToString() =>
        $"MetricsCollectorWorker {{ Interval = {Interval}, Timestamp = {_latestSnapshot.Timestamp}, TotalActors = {_latestSnapshot.TotalActors}, HealthyActors = {_latestSnapshot.HealthyActors}, ErrorActors = {_latestSnapshot.ErrorActors}, TotalMessages = {_latestSnapshot.TotalMessages} }}";
}

/// <summary>
/// Snapshot of system metrics at a point in time.
/// </summary>
public class MetricsSnapshot
{
    /// <summary>
    /// Gets or sets the UTC time at which the metrics were collected.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the total number of actors in the system.
    /// </summary>
    public int TotalActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors reporting a healthy state.
    /// </summary>
    public int HealthyActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors reporting an error state.
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
    /// Gets or sets the average message latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the error rate as a percentage.
    /// </summary>
    public double ErrorRate { get; set; }

    // Configurable threshold – default is 5.0 (percent).
    /// <summary>
    /// Gets or sets the error rate threshold, as a percentage, above which the system is unhealthy.
    /// </summary>
    public double ErrorRateThreshold { get; set; } = 5.0;

    /// <summary>
    /// Gets a value indicating whether the system is healthy: no error actors and an error rate below the threshold.
    /// </summary>
    public bool IsHealthy => ErrorActors == 0 && ErrorRate < ErrorRateThreshold;
}

/// <summary>
/// Background worker that monitors and cleans up dead letter queues.
/// </summary>
public class DeadLetterQueueWorker : IBackgroundWorker
{
    private static readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DeadLetterRetention = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the unique identifier of this worker.
    /// </summary>
    public string WorkerId => "dead-letter-queue";

    /// <summary>
    /// Gets or sets the interval between cleanup runs. Defaults to one minute.
    /// </summary>
    public TimeSpan Interval { get; set; } = DefaultCleanupInterval;

    private readonly DeadLetterQueue _deadLetterQueue;
    private readonly int _maxQueueSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterQueueWorker"/> class.
    /// </summary>
    /// <param name="deadLetterQueue">The dead letter queue to monitor and clean up.</param>
    /// <param name="maxQueueSize">The maximum number of messages to keep in the queue.</param>
    public DeadLetterQueueWorker(DeadLetterQueue deadLetterQueue, int maxQueueSize = 10000)
    {
        ArgumentNullException.ThrowIfNull(deadLetterQueue);

        _deadLetterQueue = deadLetterQueue;
        _maxQueueSize = maxQueueSize;
    }

    /// <summary>
    /// Removes dead letters older than one hour and trims the queue to the maximum size.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the cleanup run.</param>
    /// <returns>A task that completes when the cleanup run has finished.</returns>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var messages = _deadLetterQueue.GetOldestMessages(100);

            foreach (var msg in messages)
            {
                if (msg.ArrivedAt.Add(DeadLetterRetention) < DateTime.UtcNow)
                {
                    _deadLetterQueue.Remove(msg.Id);
                }
            }

            if (_deadLetterQueue.Count > _maxQueueSize)
            {
                var excess = _deadLetterQueue.Count - _maxQueueSize;
                var toRemove = _deadLetterQueue.GetOldestMessages(excess);
                foreach (var msg in toRemove)
                {
                    _deadLetterQueue.Remove(msg.Id);
                }
            }
        }, cancellationToken);
    }
}

/// <summary>
/// Dead letter queue for messages that cannot be delivered.
/// </summary>
public class DeadLetterQueue
{
    private readonly List<DeadLetteredMessage> _messages = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Gets the number of messages currently in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _messages.Count;
            }
        }
    }

    /// <summary>
    /// Adds a message to the dead letter queue.
    /// </summary>
    public void Add(Envelope envelope, string reason)
    {
        if (envelope == null) return;

        lock (_lockObject)
        {
            _messages.Add(new DeadLetteredMessage
            {
                Id = Guid.NewGuid(),
                Envelope = envelope,
                Reason = reason,
                ArrivedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Gets the oldest messages in the queue.
    /// </summary>
    public List<DeadLetteredMessage> GetOldestMessages(int count)
    {
        lock (_lockObject)
        {
            return _messages.OrderBy(m => m.ArrivedAt).Take(count).ToList();
        }
    }

    /// <summary>
    /// Removes a message by ID.
    /// </summary>
    public bool Remove(Guid messageId)
    {
        lock (_lockObject)
        {
            var msg = _messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
                return _messages.Remove(msg);
            return false;
        }
    }

    /// <summary>
    /// Clears the queue.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _messages.Clear();
        }
    }
}

/// <summary>
/// Represents a message that could not be delivered.
/// </summary>
public class DeadLetteredMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the dead-lettered message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the envelope that could not be delivered.
    /// </summary>
    public Envelope Envelope { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reason the message could not be delivered.
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC time at which the message was added to the queue.
    /// </summary>
    public DateTime ArrivedAt { get; set; }
}
