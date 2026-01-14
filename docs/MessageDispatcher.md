# MessageDispatcher

`MessageDispatcher` is the central message routing and delivery component in the `dotnet-actor-framework`. It manages the lifecycle of message delivery to actors, handling asynchronous dispatch, broadcasting, control messages, dead letter collection, and operational statistics. It serves as the backbone for reliable intra-actor communication, ensuring messages are delivered, failures are tracked, and undeliverable messages are quarantined for inspection.

## API

### public MessageDispatcher

The default constructor. Initializes a new instance of the dispatcher with empty dead letter storage and zeroed statistics. No configuration or external dependencies are required.

### public async Task<bool> DispatchAsync

Attempts to deliver a single envelope to its intended recipient.

**Parameters:**
- `Envelope envelope` — The message envelope containing the target actor reference, payload, and metadata.

**Returns:**
- `Task<bool>` — `true` if the message was successfully delivered and processed by the target actor; `false` if delivery failed or the actor rejected the message.

**Exceptions:**
- `ArgumentNullException` — Thrown when `envelope` is `null`.
- `ObjectDisposedException` — Thrown if the dispatcher has been disposed.

### public async Task SendAsync

Sends a message to a specific actor by its identifier. This overload accepts a raw payload and constructs the envelope internally.

**Parameters:**
- `ActorId target` — The unique identifier of the target actor.
- `object message` — The payload to deliver.

**Returns:**
- `Task` — Completes when the send operation has been handed off. Does not indicate delivery success; use `DispatchAsync` for confirmed delivery.

**Exceptions:**
- `ArgumentNullException` — Thrown when `target` or `message` is `null`.
- `ActorNotFoundException` — Thrown when no actor matching `target` is registered in the current actor system.

### public async Task SendAsync

Sends a pre-constructed envelope directly. This overload bypasses internal envelope creation.

**Parameters:**
- `Envelope envelope` — The fully constructed message envelope.

**Returns:**
- `Task` — Completes when the send operation has been handed off.

**Exceptions:**
- `ArgumentNullException` — Thrown when `envelope` is `null`.

### public async Task BroadcastAsync

Delivers a single message to all actors currently registered in the system.

**Parameters:**
- `object message` — The payload to broadcast.
- `BroadcastStrategy strategy` — Specifies how the broadcast is executed (e.g., parallel, sequential, fire-and-forget).

**Returns:**
- `Task` — Completes when the broadcast operation has been initiated according to the chosen strategy.

**Exceptions:**
- `ArgumentNullException` — Thrown when `message` or `strategy` is `null`.

### public async Task PublishControlAsync

Publishes a system-level control message to the actor system's control channel. Control messages are used for lifecycle management, supervision commands, and system-wide signals.

**Parameters:**
- `ControlMessage message` — The control payload (e.g., restart, stop, poison pill).

**Returns:**
- `Task` — Completes when the control message has been published to the control channel.

**Exceptions:**
- `ArgumentNullException` — Thrown when `message` is `null`.
- `InvalidOperationException` — Thrown when the control channel is unavailable or the dispatcher is in a state that cannot accept control messages.

### public async Task<Envelope?> GetNextMessageAsync

Retrieves the next pending message from the dispatcher's internal queue. Used by actors that pull messages rather than having them pushed.

**Parameters:**
- `CancellationToken cancellationToken` — Allows cancellation of the wait operation.

**Returns:**
- `Task<Envelope?>` — The next available envelope, or `null` if the queue is empty and the cancellation token is signaled or the dispatcher is drained.

**Exceptions:**
- `OperationCanceledException` — Thrown when `cancellationToken` is canceled.
- `ObjectDisposedException` — Thrown if the dispatcher has been disposed.

### public IReadOnlyList<Envelope> GetDeadLetters

Returns a snapshot of all messages that could not be delivered and were moved to the dead letter queue.

**Returns:**
- `IReadOnlyList<Envelope>` — An immutable list of dead letter envelopes. Returns an empty list if no failures have occurred.

### public DispatcherStatistics GetStatistics

Returns a snapshot of the dispatcher's current operational statistics.

**Returns:**
- `DispatcherStatistics` — A structure containing aggregated metrics including total delivered, failed, processed counts, dead letter count, and success rate.

### public long TotalDelivered

Gets the cumulative count of messages successfully delivered since the dispatcher was initialized.

### public long TotalFailed

Gets the cumulative count of messages that failed delivery since the dispatcher was initialized.

### public long TotalProcessed

Gets the cumulative count of messages that have been processed (delivered or explicitly failed) since initialization. This equals `TotalDelivered + TotalFailed`.

### public int DeadLetterCount

Gets the current number of messages residing in the dead letter queue.

### public double SuccessRate

Gets the ratio of successful deliveries to total processed messages, expressed as a value between `0.0` and `1.0`. Returns `1.0` if no messages have been processed yet.

## Usage

### Example 1: Dispatching with Confirmation and Dead Letter Inspection

```csharp
var dispatcher = new MessageDispatcher();
var envelope = new Envelope(targetActorId, new GreetingMessage("Hello"));

bool delivered = await dispatcher.DispatchAsync(envelope);

if (!delivered)
{
    var deadLetters = dispatcher.GetDeadLetters();
    foreach (var dead in deadLetters)
    {
        Console.WriteLine($"Undelivered to {dead.Target}: {dead.Payload}");
    }
}

Console.WriteLine($"Success rate: {dispatcher.SuccessRate:P}");
```

### Example 2: Broadcasting and Monitoring Statistics

```csharp
var dispatcher = new MessageDispatcher();
var announcement = new SystemAnnouncement("Maintenance window starting");

await dispatcher.BroadcastAsync(announcement, BroadcastStrategy.Parallel);

var stats = dispatcher.GetStatistics();
Console.WriteLine($"Total delivered: {stats.TotalDelivered}");
Console.WriteLine($"Total failed: {stats.TotalFailed}");
Console.WriteLine($"Dead letters: {stats.DeadLetterCount}");

if (stats.SuccessRate < 0.95)
{
    await dispatcher.PublishControlAsync(new AlertControlMessage("Low delivery success rate"));
}
```

## Notes

- **Thread Safety:** All public methods are thread-safe and may be called concurrently from multiple actors or system components. Internal state transitions and queue operations are synchronized.
- **Dead Letter Accumulation:** Dead letters are retained until explicitly cleared through the actor system's maintenance cycle. Long-running systems should periodically drain or archive dead letters to prevent unbounded memory growth.
- **`GetNextMessageAsync` and Queue Drain:** When the dispatcher is drained (no more messages expected), `GetNextMessageAsync` returns `null` rather than blocking indefinitely. Callers should handle the `null` case to exit their message processing loops gracefully.
- **`SuccessRate` Edge Case:** When `TotalProcessed` is zero, `SuccessRate` returns `1.0` to avoid division by zero. This represents a pristine state rather than perfect delivery.
- **`DispatchAsync` vs `SendAsync`:** `DispatchAsync` provides delivery confirmation and is suitable for request-response patterns. `SendAsync` is fire-and-forget and should be used when delivery acknowledgment is not required.
- **Control Message Ordering:** `PublishControlAsync` guarantees ordering of control messages relative to each other but not relative to standard messages dispatched concurrently. System components relying on causal ordering should implement their own sequencing.
