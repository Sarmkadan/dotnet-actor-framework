// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Middleware;

namespace DotNetActorFramework.BackgroundWorkers;

/// <summary>
/// Background worker that periodically collects and aggregates metrics from the actor system.
/// Provides health monitoring and performance analysis capabilities.
/// </summary>
public class MetricsCollectorWorker : IBackgroundWorker
{
    public string WorkerId => "metrics-collector";
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    private readonly ActorSystem _actorSystem;
    private readonly MetricsCollector _metricsCollector;
    private readonly MetricsSnapshot _latestSnapshot = new();

    public MetricsCollectorWorker(ActorSystem actorSystem, MetricsCollector metricsCollector)
    {
        _actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }

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

        }, cancellationToken);
    }

    /// <summary>
    /// Gets the latest collected metrics snapshot.
    /// </summary>
    public MetricsSnapshot GetLatestSnapshot() => _latestSnapshot;
}

/// <summary>
/// Snapshot of system metrics at a point in time.
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int TotalActors { get; set; }
    public int HealthyActors { get; set; }
    public int ErrorActors { get; set; }
    public long TotalMessages { get; set; }
    public long TotalErrors { get; set; }
    public double AverageLatencyMs { get; set; }
    public double ErrorRate { get; set; }

    public bool IsHealthy => ErrorActors == 0 && ErrorRate < 5.0;
}

/// <summary>
/// Background worker that monitors and cleans up dead letter queues.
/// </summary>
public class DeadLetterQueueWorker : IBackgroundWorker
{
    public string WorkerId => "dead-letter-queue";
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    private readonly DeadLetterQueue _deadLetterQueue;
    private readonly int _maxQueueSize;

    public DeadLetterQueueWorker(DeadLetterQueue deadLetterQueue, int maxQueueSize = 10000)
    {
        _deadLetterQueue = deadLetterQueue ?? throw new ArgumentNullException(nameof(deadLetterQueue));
        _maxQueueSize = maxQueueSize;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var messages = _deadLetterQueue.GetOldestMessages(100);

            foreach (var msg in messages)
            {
                if (msg.ArrivedAt.AddHours(1) < DateTime.UtcNow)
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
    public Guid Id { get; set; }
    public Envelope Envelope { get; set; }
    public string Reason { get; set; }
    public DateTime ArrivedAt { get; set; }
}
