# MetricsCollectionMiddleware

A middleware component for the `dotnet-actor-framework` that collects and tracks metrics related to message processing within an actor system. It records message counts, latencies, error rates, and system-level resource usage, providing aggregated metrics for actors, message types, and overall system performance. This middleware is designed to be inserted into the actor pipeline to monitor throughput and identify performance bottlenecks without altering message processing behavior.

## API

### `MetricsCollectionMiddleware`

Initializes a new instance of the `MetricsCollectionMiddleware` with default or specified configuration.

- **Parameters**:
  - `messageType` (string, optional): A label identifying the type of messages processed by this instance. Used for categorization in aggregated metrics.
- **Remarks**: The middleware starts with zeroed metrics counters and default system metrics.

### `async Task<bool> InvokeAsync`

Invokes the middleware pipeline, recording metrics before and after message processing.

- **Parameters**:
  - `context` (ActorMessageContext): The context containing the message and actor state.
  - `next` (Func<ActorMessageContext, Task<bool>>): The delegate representing the next middleware in the pipeline.
- **Return value**: `Task<bool>` indicating whether the pipeline execution succeeded.
- **Remarks**: Measures total latency for the message processing pipeline. Increments processed count on success and error count on failure. Throws `ArgumentNullException` if `context` or `next` is null.

### `void RecordMessageProcessed`

Records the completion of a message processing operation, updating latency and success metrics.

- **Parameters**:
  - `isSuccess` (bool): Indicates whether the message was processed successfully.
  - `latencyMs` (long): The processing latency in milliseconds.
- **Remarks**: Increments `ProcessedCount` if `isSuccess` is true; otherwise increments `ErrorCount`. Adds `latencyMs` to `TotalLatencyMs`. Does not throw.

### `MessageTypeMetrics? GetMessageTypeMetrics`

Retrieves aggregated metrics for the specified message type.

- **Parameters**:
  - `messageType` (string): The message type identifier to query.
- **Return value**: `MessageTypeMetrics?` containing aggregated counts, latency, and error rate for the message type, or `null` if no metrics exist.
- **Remarks**: Returns a snapshot of metrics at the time of invocation. Does not throw.

### `ActorMetrics? GetActorMetrics`

Retrieves aggregated metrics for the actor associated with this middleware instance.

- **Return value**: `ActorMetrics?` containing aggregated counts, latency, and error rate for the actor, or `null` if no metrics exist.
- **Remarks**: Returns a snapshot of metrics at the time of invocation. Does not throw.

### `IReadOnlyList<MessageTypeMetrics> GetAllMessageMetrics`

Retrieves aggregated metrics for all message types tracked by this middleware.

- **Return value**: `IReadOnlyList<MessageTypeMetrics>` of all message type metrics, ordered by message type.
- **Remarks**: Returns a snapshot of all metrics at the time of invocation. Does not throw.

### `IReadOnlyList<ActorMetrics> GetAllActorMetrics`

Retrieves aggregated metrics for all actors tracked by this middleware.

- **Return value**: `IReadOnlyList<ActorMetrics>` of all actor metrics, ordered by actor path.
- **Remarks**: Returns a snapshot of all metrics at the time of invocation. Does not throw.

### `SystemMetrics GetSystemMetrics`

Retrieves current system-level resource usage metrics.

- **Return value**: `SystemMetrics` containing CPU usage, memory consumption, and other system-level indicators.
- **Remarks**: Returns a snapshot of system metrics at the time of invocation. Does not throw.

### `void Reset`

Resets all recorded metrics to zero, clearing historical data.

- **Remarks**: Resets processed counts, error counts, total latency, and system metrics. Does not throw.

### `string MessageType`

Gets the message type label associated with this middleware instance.

- **Return value**: `string` representing the message type.
- **Remarks**: Read-only property. Does not throw.

### `long ProcessedCount`

Gets the total number of messages processed successfully by this middleware instance.

- **Return value**: `long` count of successful message processing operations.
- **Remarks**: Read-only property. Does not throw.

### `long ErrorCount`

Gets the total number of messages that resulted in errors during processing by this middleware instance.

- **Return value**: `long` count of failed message processing operations.
- **Remarks**: Read-only property. Does not throw.

### `long TotalLatencyMs`

Gets the cumulative latency, in milliseconds, of all processed messages.

- **Return value**: `long` total latency across all messages.
- **Remarks**: Read-only property. Does not throw.

### `double GetAverageLatencyMs`

Calculates the average processing latency in milliseconds.

- **Return value**: `double` representing the average latency, or `0` if no messages have been processed.
- **Remarks**: Does not throw.

### `double GetErrorRate`

Calculates the error rate as a percentage of total messages processed.

- **Return value**: `double` representing the error rate (0 to 100), or `0` if no messages have been processed.
- **Remarks**: Does not throw.

### `string ActorPath`

Gets the actor path associated with this middleware instance.

- **Return value**: `string` representing the actor's unique path.
- **Remarks**: Read-only property. Does not throw.

### `long ProcessedCount` (in `ActorMetrics`)

Gets the total number of messages processed successfully by the actor associated with these metrics.

- **Return value**: `long` count of successful message processing operations.
- **Remarks**: Read-only property. Does not throw.

### `long ErrorCount` (in `ActorMetrics`)

Gets the total number of messages that resulted in errors during processing by the actor.

- **Return value**: `long` count of failed message processing operations.
- **Remarks**: Read-only property. Does not throw.

### `long TotalLatencyMs` (in `ActorMetrics`)

Gets the cumulative latency, in milliseconds, of all messages processed by the actor.

- **Return value**: `long` total latency across all messages.
- **Remarks**: Read-only property. Does not throw.

### `double GetAverageLatencyMs` (in `ActorMetrics`)

Calculates the average processing latency in milliseconds for the actor.

- **Return value**: `double` representing the average latency, or `0` if no messages have been processed.
- **Remarks**: Does not throw.

## Usage
