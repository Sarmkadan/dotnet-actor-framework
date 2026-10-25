// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Repository;

/// <summary>
/// Repository for persisting and querying actor metrics.
/// Enables historical metrics analysis and performance tracking.
/// </summary>
public class ActorMetricsRepository
{
    private readonly ConnectionManager _connectionManager;
    private readonly Dictionary<Guid, List<MetricsSnapshot>> _metricsHistory = [];
    private readonly object _lockObject = new();

    protected ActorMetricsRepository()
    {
        _connectionManager = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorMetricsRepository"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager used by the repository.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public ActorMetricsRepository(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    /// <summary>
    /// Returns a string representation of the repository.
    /// </summary>
    /// <returns>A string that identifies the repository type.</returns>
    public override string ToString() => $"ActorMetricsRepository {{ ActorId = Guid.Empty, ActorPath = string.Empty, MessageCount = 0L, ProcessedCount = 0L, ErrorCount = 0L, ErrorRate = 0.0 }}";

    /// <summary>
    /// Records metrics for an actor.
    /// </summary>
    public async Task<bool> RecordMetricsAsync(Guid actorId, ActorMetricsSummary summary)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (summary == null)
            throw new ArgumentNullException(nameof(summary));

        try
        {
            var snapshot = new MetricsSnapshot
            {
                ActorId = actorId,
                ActorPath = summary.ActorPath.Path,
                MessageCount = summary.MessageCount,
                ProcessedCount = summary.ProcessedCount,
                ErrorCount = summary.ErrorCount,
                ErrorRate = summary.ErrorRate,
                SuccessRate = summary.SuccessRate,
                AverageProcessingTimeMs = summary.AverageProcessingTimeMs,
                RecordedAt = DateTime.UtcNow
            };

            lock (_lockObject)
            {
                if (!_metricsHistory.ContainsKey(actorId))
                {
                    _metricsHistory[actorId] = [];
                }

                _metricsHistory[actorId].Add(snapshot);

                // Keep only the last 1000 snapshots per actor
                if (_metricsHistory[actorId].Count > 1000)
                {
                    _metricsHistory[actorId].RemoveAt(0);
                }
            }

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets metrics history for an actor.
    /// </summary>
    public Task<IReadOnlyList<MetricsSnapshot>> GetHistoryAsync(Guid actorId, int limit = 100)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));

        IReadOnlyList<MetricsSnapshot> result1;
        lock (_lockObject)
        {
            if (_metricsHistory.TryGetValue(actorId, out var snapshots))
            {
                result1 = snapshots
                    .OrderByDescending(s => s.RecordedAt)
                    .Take(limit)
                    .ToList()
                    .AsReadOnly();
                return Task.FromResult(result1);
            }
        }

        return Task.FromResult<IReadOnlyList<MetricsSnapshot>>([]);
    }

    /// <summary>
    /// Gets metrics for a time range.
    /// </summary>
    public Task<IReadOnlyList<MetricsSnapshot>> GetMetricsAsync(Guid actorId, DateTime from, DateTime to)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (from > to)
            throw new ArgumentException("From time must be before to time.", nameof(from));

        IReadOnlyList<MetricsSnapshot> result2;
        lock (_lockObject)
        {
            if (_metricsHistory.TryGetValue(actorId, out var snapshots))
            {
                result2 = snapshots
                    .Where(s => s.RecordedAt >= from && s.RecordedAt <= to)
                    .OrderBy(s => s.RecordedAt)
                    .ToList()
                    .AsReadOnly();
                return Task.FromResult(result2);
            }
        }

        return Task.FromResult<IReadOnlyList<MetricsSnapshot>>([]);
    }

    /// <summary>
    /// Gets aggregate metrics across all actors.
    /// </summary>
    public Task<AggregateMetrics> GetAggregateMetricsAsync()
    {
        AggregateMetrics aggregate;
        lock (_lockObject)
        {
            var allSnapshots = _metricsHistory.Values
                .SelectMany(s => s)
                .OrderByDescending(s => s.RecordedAt)
                .FirstOrDefault();

            aggregate = new AggregateMetrics
            {
                TotalActorsTracked = _metricsHistory.Count,
                TotalSnapshots = _metricsHistory.Values.Sum(s => s.Count),
                RecordedAt = DateTime.UtcNow
            };

            if (allSnapshots != null)
            {
                var latest = _metricsHistory.Values
                    .Select(s => s.OrderByDescending(m => m.RecordedAt).FirstOrDefault())
                    .Where(s => s != null)
                    .Cast<MetricsSnapshot>()
                    .ToList();

                if (latest.Count > 0)
                {
                    aggregate.TotalMessages = latest.Sum(s => s.MessageCount);
                    aggregate.TotalErrors = latest.Sum(s => s.ErrorCount);
                    aggregate.AverageErrorRate = latest.Average(s => s.ErrorRate);
                    aggregate.AverageProcessingTimeMs = latest.Average(s => s.AverageProcessingTimeMs);
                }
            }
        }

        return Task.FromResult(aggregate);
    }

    /// <summary>
    /// Gets metrics snapshots for multiple actors.
    /// </summary>
    public Task<IReadOnlyList<MetricsSnapshot>> GetLatestSnapshotsAsync()
    {
        IReadOnlyList<MetricsSnapshot> latest;
        lock (_lockObject)
        {
            latest = _metricsHistory.Values
                .Select(s => s.OrderByDescending(m => m.RecordedAt).FirstOrDefault())
                .Where(s => s != null)
                .Cast<MetricsSnapshot>()
                .ToList()
                .AsReadOnly();
        }

        return Task.FromResult(latest);
    }

    /// <summary>
    /// Clears metrics history for an actor.
    /// </summary>
    public void ClearHistory(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        lock (_lockObject)
        {
            _metricsHistory.Remove(actorId);
        }
    }

    /// <summary>
    /// Clears all metrics history.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _metricsHistory.Clear();
        }
    }
}

/// <summary>
/// A snapshot of actor metrics at a specific point in time.
/// </summary>
public class MetricsSnapshot
{
    /// <summary>
    /// Gets or sets the identifier of the actor the snapshot belongs to.
    /// </summary>
    public Guid ActorId { get; set; }

    /// <summary>
    /// Gets or sets the path of the actor the snapshot belongs to.
    /// </summary>
    public string ActorPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of messages received by the actor.
    /// </summary>
    public long MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the number of messages processed by the actor.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of errors raised by the actor.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the ratio of errors to processed messages.
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the ratio of successfully processed messages.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average time taken to process a message, in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the snapshot was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Aggregate metrics across all actors.
/// </summary>
public class AggregateMetrics
{
    /// <summary>
    /// Gets or sets the number of actors that have recorded metrics.
    /// </summary>
    public int TotalActorsTracked { get; set; }

    /// <summary>
    /// Gets or sets the total number of stored metrics snapshots across all actors.
    /// </summary>
    public int TotalSnapshots { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages across the latest snapshot of each actor.
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the total number of errors across the latest snapshot of each actor.
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets the average error rate across the latest snapshot of each actor.
    /// </summary>
    public double AverageErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the average processing time, in milliseconds, across the latest snapshot of each actor.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the aggregate was computed.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Calculates a health score from the average error rate.
    /// </summary>
    /// <returns>A value from 0 to 100, where 100 means no errors.</returns>
    public double GetHealthScore()
    {
        return Math.Max(0, 100 - AverageErrorRate);
    }
}
