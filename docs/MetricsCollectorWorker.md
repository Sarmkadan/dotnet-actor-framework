# MetricsCollectorWorker

The `MetricsCollectorWorker` is a background service component within the `dotnet-actor-framework` designed to periodically aggregate runtime statistics from the actor system. It operates on a configurable time interval to collect data points such as actor health status, message throughput, error rates, and latency metrics, storing them in a snapshot format for retrieval. Additionally, this type incorporates functionality for managing a Dead Letter Queue, allowing for the tracking, retrieval, and removal of messages that failed to be delivered to their intended actors.

## API

### Properties

*   **`public TimeSpan Interval`**
    Gets or sets the time interval between consecutive metric collection cycles. This value dictates the frequency at which the `ExecuteAsync` loop runs.

*   **`public DateTime Timestamp`**
    Gets the timestamp of the most recent metrics snapshot. This indicates when the current values for total actors, messages, and errors were last calculated.

*   **`public int TotalActors`**
    Gets the total count of active actors in the system at the time of the last snapshot.

*   **`public int HealthyActors`**
    Gets the count of actors currently reported as healthy (operational without recent errors) in the last snapshot.

*   **`public int ErrorActors`**
    Gets the count of actors currently flagged with errors in the last snapshot.

*   **`public long TotalMessages`**
    Gets the cumulative count of messages processed by the system up to the last snapshot.

*   **`public long TotalErrors`**
    Gets the cumulative count of errors encountered by the system up to the last snapshot.

*   **`public double AverageLatencyMs`**
    Gets the calculated average message processing latency in milliseconds based on the data collected in the last interval.

*   **`public double ErrorRate`**
    Gets the calculated ratio of errors to total messages (typically expressed as a decimal or percentage) for the last snapshot.

*   **`public Guid Id`**
    Gets the unique identifier assigned to this specific worker instance.

*   **`public DeadLetterQueueWorker DeadLetterQueueWorker`**
    Gets the associated `DeadLetterQueueWorker` instance used to handle undeliverable messages. Note that this property exposes the worker responsible for DLQ operations, which shares the execution context.

### Constructors

*   **`public MetricsCollectorWorker()`**
    Initializes a new instance of the `MetricsCollectorWorker` class with default configuration settings.

### Methods

*   **`public async Task ExecuteAsync(CancellationToken cancellationToken = default)`**
    The primary entry point for the background service loop. This method runs asynchronously, waiting for the duration specified by `Interval` before triggering a new metrics collection cycle and updating the public statistical properties.
    *   **Parameters**: `cancellationToken` - A token to signal the request to stop the background operation.
    *   **Returns**: A `Task` that completes when the service is stopped or disposed.
    *   **Throws**: May throw `OperationCanceledException` if the cancellation token is triggered.

*   **`public MetricsSnapshot GetLatestSnapshot()`**
    Retrieves a immutable copy of the current system metrics.
    *   **Returns**: A `MetricsSnapshot` object containing the current values of all statistical properties.
    *   **Throws**: None.

*   **`public void Add(DeadLetteredMessage message)`**
    Adds a failed message to the internal Dead Letter Queue managed by this worker.
    *   **Parameters**: `message` - The `DeadLetteredMessage` instance to enqueue.
    *   **Returns**: None.
    *   **Throws**: May throw `ArgumentNullException` if `message` is null.

*   **`public List<DeadLetteredMessage> GetOldestMessages(int count)`**
    Retrieves a list of the oldest messages currently stored in the Dead Letter Queue.
    *   **Parameters**: `count` - The maximum number of messages to retrieve.
    *   **Returns**: A `List<DeadLetteredMessage>` containing the requested messages, ordered by arrival time (oldest first). Returns an empty list if the queue is empty.
    *   **Throws**: May throw `ArgumentOutOfRangeException` if `count` is negative.

*   **`public bool Remove(Guid messageId)`**
    Attempts to remove a specific message from the Dead Letter Queue by its unique identifier.
    *   **Parameters**: `messageId` - The `Guid` of the message to remove.
    *   **Returns**: `true` if the message was found and removed; `false` otherwise.
    *   **Throws**: None.

*   **`public void Clear()`**
    Removes all messages from the Dead Letter Queue.
    *   **Returns**: None.
    *   **Throws**: None.

## Usage

### Example 1: Configuring and Running the Collector
This example demonstrates how to instantiate the worker, configure the collection interval, and run it as a background task while monitoring the latest snapshot.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetActorFramework;

public class MonitoringService
{
    public async Task RunMonitoringAsync()
    {
        var collector = new MetricsCollectorWorker();
        
        // Configure the collection interval to 5 seconds
        collector.Interval = TimeSpan.FromSeconds(5);

        // Define a cancellation token for graceful shutdown
        using var cts = new CancellationTokenSource();
        
        // Start the background execution
        var executionTask = collector.ExecuteAsync(cts.Token);

        // Simulate running for 20 seconds while printing stats
        for (int i = 0; i < 4; i++)
        {
            await Task.Delay(5000);
            
            var snapshot = collector.GetLatestSnapshot();
            Console.WriteLine($"[{snapshot.Timestamp}] Actors: {collector.TotalActors} | " +
                              $"Errors: {collector.TotalErrors} | " +
                              $"Avg Latency: {collector.AverageLatencyMs:F2}ms");
        }

        // Stop the collector
        cts.Cancel();
        await executionTask;
    }
}
```

### Example 2: Managing Dead Letter Messages
This example illustrates how to use the integrated Dead Letter Queue functionality to add failed messages, retrieve the oldest entries for analysis, and clear the queue.

```csharp
using System;
using System.Collections.Generic;
using DotNetActorFramework;

public class ErrorHandler
{
    private readonly MetricsCollectorWorker _collector;

    public ErrorHandler(MetricsCollectorWorker collector)
    {
        _collector = collector;
    }

    public void ProcessFailedMessage(string content, Exception ex)
    {
        var deadMessage = new DeadLetteredMessage
        {
            Id = Guid.NewGuid(),
            Content = content,
            FailureReason = ex.Message,
            Timestamp = DateTime.UtcNow
        };

        // Add to the DLQ via the collector
        _collector.Add(deadMessage);
    }

    public List<DeadLetteredMessage> AuditOldestFailures(int limit)
    {
        // Retrieve the oldest 'limit' messages
        return _collector.GetOldestMessages(limit);
    }

    public void PurgeResolvedIssues()
    {
        // Clear the entire queue after issues are resolved
        _collector.Clear();
    }
    
    public bool AcknowledgeAndRemove(Guid messageId)
    {
        return _collector.Remove(messageId);
    }
}
```

## Notes

*   **Thread Safety**: The properties exposing metrics (e.g., `TotalActors`, `AverageLatencyMs`) are updated atomically at the end of each `ExecuteAsync` cycle. While reading these properties is generally safe, rapid updates during a collection cycle may result in reading slightly stale data. The Dead Letter Queue methods (`Add`, `Remove`, `Clear`) should be considered thread-safe for concurrent access, but external synchronization is recommended if complex multi-step operations involving multiple DLQ calls are required.
*   **Interval Configuration**: Setting `Interval` to `TimeSpan.Zero` or a negative value may result in high CPU usage due to tight looping in `ExecuteAsync`. It is recommended to set a minimum interval of 100ms.
*   **Snapshot Consistency**: The `GetLatestSnapshot` method returns a point-in-time copy of the data. Properties accessed directly on the worker (e.g., `collector.TotalMessages`) immediately after calling `GetLatestSnapshot` should match the snapshot values, provided no new collection cycle has completed in the interim.
*   **Dead Letter Queue Limits**: The implementation does not explicitly enforce a maximum queue size in the provided signature. Consumers should monitor the queue depth using `GetOldestMessages` or custom logic and invoke `Clear()` or `Remove()` periodically to prevent unbounded memory growth in long-running applications.
*   **Execution Lifecycle**: The `ExecuteAsync` method is designed to run for the lifetime of the application. It respects the provided `CancellationToken` and will terminate gracefully when cancelled. Restarting the worker requires instantiating a new object or implementing external restart logic, as no `Start`/`Stop` methods are exposed beyond the async task lifecycle.
