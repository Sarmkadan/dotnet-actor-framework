# IntegrationEventPublisher

The `IntegrationEventPublisher` class represents a single integration event that is queued for publication. It encapsulates the event payload, its unique identifier, enqueue and processing timestamps, and the number of publication attempts. The class also provides methods to publish the event asynchronously, inspect the current queue length, and manage deduplication through a dedicated `DuplicateEventFilteringPublisher` instance. This design allows each event to be tracked and published independently while sharing a deduplication cache.

## API

### `public IntegrationEventPublisher`

Initializes a new instance of the `IntegrationEventPublisher`.  
*Parameters:* None (or implicit – see implementation).  
*Returns:* Nothing (constructor).  
*Throws:* None.

### `public Task PublishAsync`

Publishes the event represented by this instance.  
*Parameters:* None.  
*Returns:* A `Task` that completes when the publish operation finishes.  
*Throws:* May throw exceptions related to network failures, serialization errors, or transient infrastructure faults.

### `public int GetQueueLength`

Returns the number of events currently pending in the publisher’s internal queue.  
*Parameters:* None.  
*Returns:* An `int` representing the queue depth.  
*Throws:* None.

### `public void Dispose`

Releases all resources used by the `IntegrationEventPublisher`.  
*Parameters:* None.  
*Returns:* Nothing.  
*Throws:* None.

### `public Guid Id`

Gets the unique identifier assigned to this event.  
*Type:* `Guid`  
*Throws:* None.

### `public IDomainEvent Event`

Gets the domain event payload that this publisher is responsible for delivering.  
*Type:* `IDomainEvent`  
*Throws:* None.

### `public DateTime EnqueuedAt`

Gets the timestamp (in UTC) when the event was enqueued for publication.  
*Type:* `DateTime`  
*Throws:* None.

### `public DateTime? ProcessedAt`

Gets the timestamp (in UTC) when the event was successfully processed, or `null` if it has not yet been processed.  
*Type:* `DateTime?`  
*Throws:* None.

### `public int Attempts`

Gets the number of times the event has been attempted for publication.  
*Type:* `int`  
*Throws:* None.

### `public DuplicateEventFilteringPublisher`

Gets the `DuplicateEventFilteringPublisher` instance used for deduplication logic.  
*Type:* `DuplicateEventFilteringPublisher`  
*Throws:* None.

### `public async Task PublishAsync<TEvent>`

Publishes an event of the specified type. This generic overload allows type‑safe publication.  
*Type parameter:* `TEvent` – the concrete type of the event (must implement `IDomainEvent`).  
*Parameters:* None (the event is taken from the instance’s `Event` property).  
*Returns:* A `Task` that completes when the publish operation finishes.  
*Throws:* May throw exceptions related to network failures, serialization errors, or transient infrastructure faults.

### `public void ClearDeduplicationCache`

Clears the deduplication cache maintained by the associated `DuplicateEventFilteringPublisher`.  
*Parameters:* None.  
*Returns:* Nothing.  
*Throws:* None.

## Usage

### Example 1: Publishing a single event

```csharp
using var publisher = new IntegrationEventPublisher();
publisher.Event = new OrderPlacedEvent { OrderId = 42 };
publisher.EnqueuedAt = DateTime.UtcNow;

await publisher.PublishAsync();

Console.WriteLine($"Published event {publisher.Id} after {publisher.Attempts} attempt(s).");
```

### Example 2: Using deduplication and clearing cache

```csharp
var publisher = new IntegrationEventPublisher();
publisher.Event = new PaymentReceivedEvent { TransactionId = "TXN-001" };

// Publish with deduplication enabled
await publisher.PublishAsync();

// Later, clear the deduplication cache to allow re‑publication
publisher.ClearDeduplicationCache();

// Publish again (will be treated as a new attempt)
await publisher.PublishAsync();

Console.WriteLine($"Queue length after second publish: {publisher.GetQueueLength()}");
```

## Notes

- **Thread safety:** The `IntegrationEventPublisher` is not guaranteed to be thread‑safe. Concurrent calls to `PublishAsync`, `GetQueueLength`, or property setters may lead to inconsistent state. Use external synchronization if the same instance is accessed from multiple threads.
- **Disposal:** After `Dispose` is called, the instance should not be used. Subsequent calls to `PublishAsync` or property accessors may throw `ObjectDisposedException`.
- **Deduplication cache:** The `DuplicateEventFilteringPublisher` instance is shared across all `IntegrationEventPublisher` instances that were created with the same underlying cache. Clearing the cache via `ClearDeduplicationCache` affects all publishers using that cache.
- **Attempts and processed state:** The `Attempts` property increments only after a call to `PublishAsync` completes (regardless of success or failure). The `ProcessedAt` property is set to `null` until the event is successfully published; it is updated to the current UTC time upon success.
- **Queue length:** `GetQueueLength` reflects the number of events that have been enqueued but not yet published. It is not affected by the state of the current instance’s own event.
- **Generic `PublishAsync<TEvent>`:** The type parameter `TEvent` must match the runtime type of the `Event` property; otherwise, the behavior is undefined.
