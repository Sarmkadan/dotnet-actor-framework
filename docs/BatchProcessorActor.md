# BatchProcessorActor

`BatchProcessorActor` is a specialized actor designed to accumulate incoming messages into configurable batches and process them collectively once a batch reaches a defined size or a flush interval elapses. It extends the base `Actor` type and overrides the core lifecycle and message-handling methods to implement deferred, grouped processing. This pattern is useful when per-message processing overhead is high and amortizing work across multiple items improves throughput or reduces external resource contention.

## API

### `public BatchProcessorActor(ActorPath path)`

**Purpose**: Initializes a new instance of `BatchProcessorActor` at the specified actor path.

**Parameters**:
- `path` (`ActorPath`): The unique address identifying this actor within the actor hierarchy. Must not be `null`.

**Return value**: A new `BatchProcessorActor` instance (constructor).

**Throws**:
- `ArgumentNullException` if `path` is `null`.

---

### `public override async Task OnInitializeAsync()`

**Purpose**: Called by the actor runtime exactly once, before the first message is delivered. Performs actor-specific startup logic such as scheduling the batch-flush timer, allocating the internal message accumulator, and validating configuration.

**Parameters**: None.

**Return value**: A `Task` that completes when initialization is finished. The actor will not begin processing messages until this task resolves.

**Throws**: Any exception thrown during initialization will fault the actor and prevent it from receiving messages. Implementations should handle recoverable errors internally and only propagate fatal failures.

---

### `public override async Task ReceiveAsync()`

**Purpose**: Invoked by the actor runtime when a message is available in the mailbox. The default implementation enqueues the incoming message into the internal batch buffer. When the buffer reaches the configured batch size, or when the flush timer fires, the accumulated batch is processed as a single unit.

**Parameters**: None. The incoming message is accessed through the actor’s mailbox context.

**Return value**: A `Task` that completes when the message has been accepted into the batch (not necessarily when the batch is processed). Batch processing itself occurs asynchronously and may extend beyond the completion of this method.

**Throws**: If batch processing logic throws, the exception is surfaced through the actor’s supervision mechanism. The actor remains alive to receive subsequent messages unless the error is classified as fatal by the supervisor.

---

### `public override async Task OnStopAsync()`

**Purpose**: Called by the actor runtime when the actor is instructed to stop. Flushes any remaining messages in the current partial batch before shutting down, ensuring no data loss. Releases timers and other resources allocated during initialization.

**Parameters**: None.

**Return value**: A `Task` that completes when the final batch has been processed and all resources have been released.

**Throws**: Exceptions during final flush are logged and swallowed to guarantee the actor terminates. They do not prevent shutdown.

## Usage

### Example 1: Basic batch insertion with size threshold

```csharp
var path = ActorPath.Parse("/processors/batch-ingestor");
var batchActor = new BatchProcessorActor(path);

// Configure batch size before initialization
batchActor.BatchSize = 100;

await batchActor.StartAsync();

// Send individual items; they accumulate internally
for (int i = 0; i < 250; i++)
{
    await batchActor.TellAsync(new LogEntry { Id = i, Payload = $"Event-{i}" });
}

// After 250 messages, two full batches of 100 are processed automatically,
// and 50 remain in the buffer until the next trigger or stop.
await batchActor.StopAsync(); // flushes remaining 50
```

### Example 2: Time-based flush combined with size threshold

```csharp
var path = ActorPath.Parse("/processors/timed-batcher");
var batchActor = new BatchProcessorActor(path);

batchActor.BatchSize = 50;
batchActor.FlushInterval = TimeSpan.FromSeconds(10);

await batchActor.StartAsync();

// Low-volume stream: messages arrive sporadically
await batchActor.TellAsync(new SensorReading { Timestamp = DateTime.UtcNow, Value = 22.4 });
await Task.Delay(TimeSpan.FromSeconds(12));
await batchActor.TellAsync(new SensorReading { Timestamp = DateTime.UtcNow, Value = 23.1 });

// The first reading triggers a flush after 10 seconds even though batch size isn't reached.
// The second reading starts a new batch.
await batchActor.StopAsync();
```

## Notes

- **Partial batches on stop**: `OnStopAsync` guarantees that any messages remaining in an incomplete batch are processed before the actor terminates. Callers do not need to manually flush.
- **Concurrent message delivery**: The actor runtime delivers messages sequentially to `ReceiveAsync`. No additional synchronization is required for the internal batch buffer. However, batch-processing logic that accesses shared external state must implement its own thread-safety measures.
- **Timer overlap**: If a flush timer fires while a batch is already being processed (e.g., due to a size-triggered flush), the implementation must guard against concurrent batch dispatch. Typically this is handled by ignoring timer ticks when the buffer is empty or already being flushed.
- **Error isolation**: An exception during batch processing does not discard the batch by default. The actor’s supervision strategy determines whether the batch is retried, skipped, or escalated. Consult the supervisor configuration to understand recovery behavior.
- **Resource cleanup**: Timers and disposable resources allocated in `OnInitializeAsync` are released in `OnStopAsync`. Failure to call `StopAsync` (or an equivalent shutdown path) may leak timers and prevent graceful process exit.
