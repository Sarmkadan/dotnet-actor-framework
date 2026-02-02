# ActorSystemDiagnostics

`ActorSystemDiagnostics` provides runtime monitoring and diagnostic capabilities for an actor system in the `dotnet-actor-framework`. It exposes performance metrics, memory usage, garbage collection statistics, actor hierarchy analysis, and message processing insights. This class is primarily used for observability, debugging, and performance tuning of actor-based applications.

## API

### `PerformanceSnapshot TakeSnapshot()`
Captures a point-in-time performance snapshot of the actor system, including message throughput, actor counts, and resource utilization. The snapshot is stored internally and can be retrieved later for analysis.
- **Returns**: A `PerformanceSnapshot` containing the current state of the system.
- **Throws**: None.

### `PerformanceSnapshot? GetLatestSnapshot()`
Retrieves the most recent performance snapshot taken by `TakeSnapshot()`.
- **Returns**: The latest `PerformanceSnapshot` if one exists; otherwise, `null`.
- **Throws**: None.

### `IReadOnlyList<PerformanceSnapshot> GetSnapshotsSince(DateTime since)`
Retrieves all performance snapshots recorded since the specified timestamp.
- **Parameters**:
  - `since` (`DateTime`): The cutoff timestamp. Snapshots taken after this time are included.
- **Returns**: A read-only list of `PerformanceSnapshot` objects. Returns an empty list if no snapshots exist since the given time.
- **Throws**: None.

### `MemoryStatistics GetMemoryStatistics()`
Returns detailed memory usage statistics for the actor system, including managed heap, working set, and private memory.
- **Returns**: A `MemoryStatistics` object containing the current memory metrics.
- **Throws**: None.

### `GcStatistics GetGcStatistics()`
Returns garbage collection statistics, including generation counts, collection frequencies, and memory pressure indicators.
- **Returns**: A `GcStatistics` object with the latest GC data.
- **Throws**: None.

### `ActorPathAnalysis AnalyzeActorHierarchy()`
Performs a structural analysis of the actor hierarchy, identifying parent-child relationships, actor counts per path, and potential bottlenecks.
- **Returns**: An `ActorPathAnalysis` object containing the hierarchy breakdown.
- **Throws**: None.

### `List<ActorLoadInfo> FindHeaviestActors(int topN = 10)`
Identifies the most resource-intensive actors based on message throughput, CPU usage, or memory consumption.
- **Parameters**:
  - `topN` (`int`, optional): The number of actors to return. Defaults to `10`.
- **Returns**: A list of `ActorLoadInfo` objects, sorted by descending load.
- **Throws**: None.

### `void ClearSnapshots()`
Removes all stored performance snapshots from memory.
- **Throws**: None.

### `DateTime Timestamp`
The timestamp of the last diagnostic update or snapshot.
- **Returns**: A `DateTime` representing the last update time.

### `int TotalActors`
The total number of actors currently active in the system, including healthy and errored actors.
- **Returns**: An `int` representing the count.

### `int HealthyActors`
The number of actors in a healthy state (not in error).
- **Returns**: An `int` representing the count.

### `int ErrorActors`
The number of actors in an error state.
- **Returns**: An `int` representing the count.

### `long TotalMessages`
The cumulative count of messages processed by the actor system since startup.
- **Returns**: A `long` representing the total.

### `long TotalErrors`
The cumulative count of message processing errors encountered since startup.
- **Returns**: A `long` representing the total.

### `long MemoryUsageMb`
The current memory usage of the actor system in megabytes, including managed and unmanaged allocations.
- **Returns**: A `long` representing the usage in MB.

### `double CpuUsagePercent`
The current CPU usage percentage of the actor system process.
- **Returns**: A `double` representing the percentage (0–100).

### `long WorkingSetMb`
The working set memory (physical memory usage) of the actor system process in megabytes.
- **Returns**: A `long` representing the usage in MB.

### `long PrivateMemoryMb`
The private memory (committed memory not shared with other processes) of the actor system process in megabytes.
- **Returns**: A `long` representing the usage in MB.

### `long ManagedHeapMb`
The size of the managed heap in megabytes.
- **Returns**: A `long` representing the heap size in MB.

## Usage

### Example 1: Monitoring Performance Metrics
