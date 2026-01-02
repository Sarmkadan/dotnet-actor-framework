# ActorMetrics

`ActorMetrics` is a class that captures runtime metrics for a single actor instance. It maintains counters for messages received, processed, and errors, along with timing information, mailbox depth, and timestamps. Derived metrics such as error rate, success rate, uptime, and a health indicator are computed from the raw counters. This type is intended to be used within the actor framework to monitor and diagnose actor behavior.

## API

### Properties

- **`public Guid ActorId`**  
  Gets the unique identifier of the actor.

- **`public ActorPath ActorPath`**  
  Gets the path of the actor within the actor hierarchy.

- **`public long MessageCount`**  
  Gets the total number of messages received by the actor.

- **`public long ErrorCount`**  
  Gets the total number of errors that occurred during message processing.

- **`public long ProcessedCount`**  
  Gets the total number of messages that have been successfully processed.

- **`public double AverageProcessingTimeMs`**  
  Gets the average processing time per message in milliseconds.

- **`public DateTime CreatedAt`**  
  Gets the timestamp when this metrics instance was created.

- **`public DateTime? LastMessageTime`**  
  Gets the timestamp of the last message received, or `null` if no message has been received.

- **`public int MailboxDepth`**  
  Gets the current depth of the actor’s mailbox.

### Constructor

- **`public ActorMetrics()`**  
  Initializes a new instance of the `ActorMetrics` class. Sets `CreatedAt` to the current UTC time and initializes all counters to zero.

### Methods

- **`public void RecordMessageReceived()`**  
  Records that a message has been received. Increments `MessageCount` and updates `LastMessageTime` to the current UTC time.

- **`public void RecordProcessingTime()`**  
  Records the processing time for the last message. Updates `AverageProcessingTimeMs` based on the internally measured elapsed time.  
  *Note: The method signature does not include a parameter; the implementation is expected to measure the time internally (e.g., using a stopwatch started before processing).*

- **`public void RecordError()`**  
  Records that an error occurred during message processing. Increments `ErrorCount`.

- **`public void UpdateMailboxDepth()`**  
  Updates `MailboxDepth` to the current mailbox depth.  
  *Note: The method signature does not include a parameter; the implementation is expected to retrieve the depth from the actor’s mailbox internally.*

- **`public double GetErrorRate()`**  
  Returns the error rate as a value between 0.0 and 1.0, calculated as `ErrorCount / MessageCount`. Returns 0.0 if `MessageCount` is zero.

- **`public double GetSuccessRate()`**  
  Returns the success rate as a value between 0.0 and 1.0, calculated as `ProcessedCount / MessageCount`. Returns 0.0 if `MessageCount` is zero.

- **`public TimeSpan GetUptime()`**  
  Returns the time elapsed since `CreatedAt` (i.e., `DateTime.UtcNow - CreatedAt`).

- **`public bool IsUnhealthy()`**  
  Returns `true` if the actor is considered unhealthy based on internal thresholds (e.g., high error rate or excessive mailbox depth), otherwise `false`.

- **`public ActorMetricsSummary GetSummary()`**  
  Returns an `ActorMetricsSummary` object that captures a snapshot of the current metrics values.

## Usage

### Example 1: Basic metrics recording and health check

```csharp
var metrics = new ActorMetrics();

// Simulate receiving and processing a message
metrics.RecordMessageReceived();
// ... process message ...
metrics.RecordProcessingTime();
metrics.RecordError(); // an error occurred

// Check health
if (metrics.IsUnhealthy())
{
    Console.WriteLine("Actor is unhealthy. Error rate: {0:P}", metrics.GetErrorRate());
}

Console.WriteLine($"Messages received: {metrics.MessageCount}");
Console.WriteLine($"Uptime: {metrics.GetUptime()}");
```

### Example 2: Periodic summary collection

```csharp
var metrics = new ActorMetrics();

// In a message handler
void HandleMessage()
{
    metrics.RecordMessageReceived();
    var sw = Stopwatch.StartNew();
    try
    {
        // process message
        metrics.RecordProcessingTime(); // assumes internal timing
        metrics.ProcessedCount++; // note: ProcessedCount is a property, not a method; increment directly
    }
    catch
    {
        metrics.RecordError();
    }
    finally
    {
        metrics.UpdateMailboxDepth();
    }
}

// Periodically log a summary
var summary = metrics.GetSummary();
Console.WriteLine($"Processed: {summary.ProcessedCount}, Errors: {summary.ErrorCount}");
```

## Notes

- **Division by zero**: `GetErrorRate()` and `GetSuccessRate()` return `0.0` when `MessageCount` is zero to avoid division by zero exceptions.
- **Thread safety**: The `ActorMetrics` class is not guaranteed to be thread-safe. Concurrent calls to recording methods from multiple threads may result in inconsistent counter values. If used in a multi-threaded actor system, external synchronization (e.g., locks) should be applied.
- **Internal timing**: `RecordProcessingTime()` and `UpdateMailboxDepth()` have no parameters; their implementations must obtain the required values from the actor’s internal state or a previously started stopwatch. The exact mechanism is not exposed by the public API.
- **`IsUnhealthy` thresholds**: The criteria for determining an unhealthy state are implementation-defined and may depend on configuration or hardcoded limits (e.g., error rate > 0.5 or mailbox depth > 1000). The method returns `false` if no thresholds are exceeded.
- **`GetSummary()`**: Returns a snapshot; subsequent changes to the metrics instance do not affect the returned `ActorMetricsSummary` object.
