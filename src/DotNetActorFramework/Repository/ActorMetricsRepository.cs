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

    public ActorMetricsRepository(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

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
    public async Task<IReadOnlyList<MetricsSnapshot>> GetHistoryAsync(Guid actorId, int limit = 100)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));

        lock (_lockObject)
        {
            if (_metricsHistory.TryGetValue(actorId, out var snapshots))
            {
                var result = snapshots
                    .OrderByDescending(s => s.RecordedAt)
                    .Take(limit)
                    .ToList()
                    .AsReadOnly();

                await Task.CompletedTask;
                return result;
            }
        }

        await Task.CompletedTask;
        return [];
    }

    /// <summary>
    /// Gets metrics for a time range.
    /// </summary>
    public async Task<IReadOnlyList<MetricsSnapshot>> GetMetricsAsync(Guid actorId, DateTime from, DateTime to)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (from > to)
            throw new ArgumentException("From time must be before to time.", nameof(from));

        lock (_lockObject)
        {
            if (_metricsHistory.TryGetValue(actorId, out var snapshots))
            {
                var result = snapshots
                    .Where(s => s.RecordedAt >= from && s.RecordedAt <= to)
                    .OrderBy(s => s.RecordedAt)
                    .ToList()
                    .AsReadOnly();

                await Task.CompletedTask;
                return result;
            }
        }

        await Task.CompletedTask;
        return [];
    }

    /// <summary>
    /// Gets aggregate metrics across all actors.
    /// </summary>
    public async Task<AggregateMetrics> GetAggregateMetricsAsync()
    {
        lock (_lockObject)
        {
            var allSnapshots = _metricsHistory.Values
                .SelectMany(s => s)
                .OrderByDescending(s => s.RecordedAt)
                .FirstOrDefault();

            var aggregate = new AggregateMetrics
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

            await Task.CompletedTask;
            return aggregate;
        }
    }

    /// <summary>
    /// Gets metrics snapshots for multiple actors.
    /// </summary>
    public async Task<IReadOnlyList<MetricsSnapshot>> GetLatestSnapshotsAsync()
    {
        lock (_lockObject)
        {
            var latest = _metricsHistory.Values
                .Select(s => s.OrderByDescending(m => m.RecordedAt).FirstOrDefault())
                .Where(s => s != null)
                .Cast<MetricsSnapshot>()
                .ToList()
                .AsReadOnly();

            await Task.CompletedTask;
            return latest;
        }
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
    public Guid ActorId { get; set; }
    public string ActorPath { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long ProcessedCount { get; set; }
    public long ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double SuccessRate { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Aggregate metrics across all actors.
/// </summary>
public class AggregateMetrics
{
    public int TotalActorsTracked { get; set; }
    public int TotalSnapshots { get; set; }
    public long TotalMessages { get; set; }
    public long TotalErrors { get; set; }
    public double AverageErrorRate { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public DateTime RecordedAt { get; set; }

    public double GetHealthScore()
    {
        return Math.Max(0, 100 - AverageErrorRate);
    }
}
