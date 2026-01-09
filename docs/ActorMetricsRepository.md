# ActorMetricsRepository

`ActorMetricsRepository` is a per-actor metrics store that records, aggregates, and exposes operational telemetry for a single actor instance. It tracks message counts, processing throughput, error rates, and timing data, and retains a configurable history of snapshots for trend analysis.

## API

### Properties

- **`Guid ActorId`**  
  The unique identifier of the actor this repository belongs to.

- **`string ActorPath`**  
  The hierarchical path of the actor within the actor system.

- **`long MessageCount`**  
  The total number of messages received by the actor since tracking began.

- **`long ProcessedCount`**  
  The number of messages that completed processing successfully.

- **`long ErrorCount`**  
  The number of messages whose processing resulted in an error.

- **`double ErrorRate`**  
  The ratio of errors to total messages, expressed as a value between 0 and 1.

- **`double SuccessRate`**  
  The ratio of successful completions to total messages, expressed as a value between 0 and 1.

- **`double AverageProcessingTimeMs`**  
  The mean processing time in milliseconds across all processed messages.

- **`DateTime RecordedAt`**  
  The timestamp of the most recent metrics recording.

- **`int TotalActorsTracked`**  
  The number of distinct actors for which metrics are being tracked in this repository instance.

- **`int TotalSnapshots`**  
  The total number of `MetricsSnapshot` entries stored in the history.

- **`long TotalMessages`**  
  The cumulative message count across all tracked actors.

### Methods

- **`public ActorMetricsRepository`**  
  Constructor. Initializes a new repository bound to a specific actor. The actor identity and path are set during construction and remain immutable for the lifetime of the instance.

- **`public async Task<bool> RecordMetricsAsync`**  
  Captures the current instantaneous metrics as a `MetricsSnapshot` and appends it to the internal history. Returns `true` if the snapshot was successfully persisted; returns `false` if the recording was rejected due to internal constraints (e.g., history capacity limits or a disabled tracking state). This method is asynchronous to accommodate potential I/O when persisting snapshots to a backing store.

- **`public Task<IReadOnlyList<MetricsSnapshot>> GetHistoryAsync`**  
  Retrieves all recorded snapshots in chronological order. The returned list is a read-only snapshot of the history at the time of the call. Returns an empty list if no recordings have been made.

- **`public Task<IReadOnlyList<MetricsSnapshot>> GetMetricsAsync`**  
  Returns the full set of snapshots currently held in memory, equivalent to `GetHistoryAsync` when no filtering is applied. Provided as a semantic alias for contexts where “metrics” is the preferred term.

- **`public Task<AggregateMetrics> GetAggregateMetricsAsync`**  
  Computes and returns aggregate statistics derived from all recorded snapshots. The `AggregateMetrics` object includes min, max, and average values for message counts, error rates, and processing times over the entire history. If no snapshots exist, the returned aggregates contain zero or default values.

- **`public Task<IReadOnlyList<MetricsSnapshot>> GetLatestSnapshotsAsync`**  
  Returns the most recent snapshots up to an internally defined limit (typically a fixed-size window). Useful for dashboards that require only recent data. The exact count returned depends on repository configuration.

- **`public void ClearHistory`**  
  Removes all recorded snapshots from the history while preserving the current live counters (`MessageCount`, `ProcessedCount`, `ErrorCount`, etc.). After this call, `TotalSnapshots` becomes zero, but the actor’s running totals remain intact.

- **`public void Clear`**  
  Resets the repository to its initial state. All history is removed and all live counters are set to zero. Equivalent to a full reset of both historical data and current accumulators.

## Usage

### Example 1: Recording and retrieving metrics for a single actor

```csharp
var repo = new ActorMetricsRepository(actorId, "/user/worker-1");

// Simulate processing outcomes
Interlocked.Increment(ref repo.MessageCount);
Interlocked.Increment(ref repo.ProcessedCount);

bool recorded = await repo.RecordMetricsAsync();
if (recorded)
{
    var history = await repo.GetHistoryAsync();
    foreach (var snapshot in history)
    {
        Console.WriteLine($"[{snapshot.RecordedAt}] Errors: {snapshot.ErrorCount}, Rate: {snapshot.ErrorRate:P}");
    }
}
```

### Example 2: Aggregating metrics across a time window

```csharp
var repo = new ActorMetricsRepository(actorId, "/user/service-a");

// After several recordings...
await repo.RecordMetricsAsync();
await repo.RecordMetricsAsync();

var aggregates = await repo.GetAggregateMetricsAsync();
Console.WriteLine($"Avg processing time: {aggregates.AverageProcessingTimeMs:F2} ms");
Console.WriteLine($"Peak error rate: {aggregates.MaxErrorRate:P}");

// Retrieve only recent snapshots for a live dashboard
var recent = await repo.GetLatestSnapshotsAsync();
foreach (var snap in recent)
{
    Console.WriteLine($"{snap.RecordedAt:T} — Success: {snap.SuccessRate:P}");
}

// Reset history but keep current counters
repo.ClearHistory();
```

## Notes

- **Thread safety:** All public methods and property accessors are safe for concurrent use. The implementation uses internal synchronization to ensure that reads and writes to counters and snapshot history are atomic from the caller’s perspective. However, individual property reads are not coordinated with `RecordMetricsAsync`; a snapshot recorded between two property accesses may reflect intermediate state.
- **History capacity:** The repository may enforce a maximum number of stored snapshots. When the limit is reached, `RecordMetricsAsync` returns `false` and the snapshot is discarded. Callers should check the return value to detect dropped recordings.
- **Aggregate calculations:** `GetAggregateMetricsAsync` operates over the entire history. If `ClearHistory` has been called, aggregates are computed from an empty set and return zero/default values rather than throwing.
- **Clear vs ClearHistory:** `Clear` resets both history and live counters; `ClearHistory` preserves the running totals so that ongoing actor operation is not disrupted. Use `Clear` only when decommissioning an actor or starting a completely new measurement period.
- **Property freshness:** The scalar properties (`MessageCount`, `ErrorRate`, etc.) reflect the live state at the moment of access. They are not snapshotted automatically. To capture a coherent point-in-time view, call `RecordMetricsAsync` and read the resulting snapshot from history.
- **Async completion:** The `Task`-returning methods complete synchronously for in-memory repositories but are declared asynchronous to support derived implementations that persist to external stores. In default configurations, continuations attached to these tasks will execute on the calling thread.
