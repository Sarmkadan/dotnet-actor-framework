# PersistenceService

The `PersistenceService` class provides the core infrastructure for persisting and retrieving actor state and event streams within the `dotnet-actor-framework`. It acts as an abstraction layer over the underlying storage mechanism, enabling actors to save snapshots for fast recovery, append domain events for event sourcing, and query historical event data. All operations are asynchronous, ensuring non-blocking I/O during persistence interactions.

## API

### Constructors

#### `public PersistenceService()`
Initializes a new instance of the `PersistenceService` class. This constructor typically sets up internal dependencies required for database or file system interaction, depending on the framework's configuration.

### Methods

#### `public async Task SaveSnapshotAsync`
Persists the current state of an actor as a snapshot.
*   **Purpose**: Reduces recovery time by allowing actors to restore state from a recent point rather than replaying the entire event log.
*   **Parameters**: Implicitly requires context regarding the actor ID and snapshot data (specific signature details depend on framework overload resolution, but generally accepts actor identifier and state object).
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws an exception if the storage backend is unavailable, serialization fails, or the actor ID is invalid.

#### `public async Task<ActorSnapshot?> LoadLatestSnapshotAsync`
Retrieves the most recent snapshot for a specific actor.
*   **Purpose**: Used during actor initialization to restore state to the last known good point.
*   **Parameters**: Requires the unique identifier of the actor.
*   **Return Value**: A `Task` yielding an `ActorSnapshot` object if a snapshot exists, or `null` if no snapshots have been saved for the actor.
*   **Throws**: Throws an exception if the storage backend is unreachable or data corruption is detected.

#### `public async Task DeleteSnapshotsAsync`
Deletes specific snapshots based on provided criteria (e.g., sequence numbers or timestamps).
*   **Purpose**: Manages storage retention by removing outdated or intermediate snapshots.
*   **Parameters**: Requires the actor ID and criteria defining which snapshots to remove.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws an exception if the deletion operation fails due to permissions or backend errors.

#### `public async Task DeleteAllSnapshotsAsync`
Removes all snapshots associated with a specific actor.
*   **Purpose**: Completely clears snapshot history for an actor, forcing future recoveries to rely solely on event replay.
*   **Parameters**: Requires the unique identifier of the actor.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws an exception if the backend operation fails.

#### `public async Task AppendEventsAsync`
Appends a collection of new events to the actor's event stream.
*   **Purpose**: The primary method for event sourcing, recording state changes as immutable events.
*   **Parameters**: Requires the actor ID and an enumerable collection of `ActorEvent` objects.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws an exception if concurrency checks fail (optimistic locking), the stream is sealed, or the write operation fails.

#### `public async Task<IEnumerable<ActorEvent>> ReadEventsAsync`
Reads events from the actor's event stream in forward chronological order.
*   **Purpose**: Replays events to reconstruct state or for auditing purposes.
*   **Parameters**: Requires the actor ID and optionally a starting sequence number or count.
*   **Return Value**: A `Task` yielding an `IEnumerable<ActorEvent>` containing the requested events.
*   **Throws**: Throws an exception if the actor ID is not found or the storage backend fails.

#### `public async Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync`
Reads events from the actor's event stream in reverse chronological order.
*   **Purpose**: Useful for debugging recent activities or implementing patterns that require the latest events first.
*   **Parameters**: Requires the actor ID and optionally a starting sequence number or count.
*   **Return Value**: A `Task` yielding an `IEnumerable<ActorEvent>` containing the requested events in reverse order.
*   **Throws**: Throws an exception if the actor ID is not found or the storage backend fails.

#### `public async Task DeleteEventsAsync`
Deletes specific events from the actor's event stream based on sequence numbers or ranges.
*   **Purpose**: Supports compliance requirements (e.g., GDPR) or log compaction strategies.
*   **Parameters**: Requires the actor ID and the specific range or identifiers of events to delete.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws an exception if the deletion violates stream integrity constraints or fails at the backend level.

#### `public async Task DeleteAllEventsAsync`
Removes the entire event stream for a specific actor.
*   **Purpose**: Completely resets the actor's history, effectively destroying its persisted state.
*   **Parameters**: Requires the unique identifier of the actor.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws an exception if the operation fails due to backend errors or protection policies.

## Usage

### Example 1: Actor Recovery and Event Replay
This example demonstrates how to initialize an actor by loading the latest snapshot and replaying subsequent events to reach the current state.

```csharp
public async Task InitializeActorAsync(string actorId, PersistenceService persistenceService)
{
    // Attempt to load the latest snapshot
    var snapshot = await persistenceService.LoadLatestSnapshotAsync(actorId);
    
    long currentSequence = 0;
    if (snapshot != null)
    {
        // Restore state from snapshot
        ApplySnapshot(snapshot.Data);
        currentSequence = snapshot.SequenceNumber;
    }

    // Read events occurring after the snapshot
    var events = await persistenceService.ReadEventsAsync(actorId, currentSequence + 1);
    
    foreach (var evt in events)
    {
        ApplyEvent(evt);
    }
}
```

### Example 2: Saving State and Appending New Events
This example shows a typical command handler workflow where an action results in new events being appended and a periodic snapshot being saved.

```csharp
public async Task ProcessCommandAsync(string actorId, Command cmd, PersistenceService persistenceService)
{
    var newEvents = new List<ActorEvent>
    {
        new ActorEvent("OrderPlaced", new { OrderId = cmd.OrderId, Timestamp = DateTime.UtcNow }),
        new ActorEvent("InventoryReserved", new { ItemId = cmd.ItemId })
    };

    // Append the new events to the stream
    await persistenceService.AppendEventsAsync(actorId, newEvents);

    // If the event count threshold is met, save a snapshot
    if (ShouldTakeSnapshot())
    {
        var currentState = GetCurrentState();
        await persistenceService.SaveSnapshotAsync(actorId, currentState);
    }
}
```

## Notes

*   **Thread Safety**: While the methods are asynchronous, `PersistenceService` instances should generally be treated as stateless or scoped per actor context. Concurrent calls to `AppendEventsAsync` for the same `actorId` from multiple threads without external synchronization may lead to race conditions or concurrency exceptions depending on the underlying storage implementation's optimistic locking strategy.
*   **Event Ordering**: `ReadEventsAsync` guarantees forward chronological order, while `ReadEventsBackwardAsync` guarantees reverse order. However, consistency is only guaranteed relative to committed transactions; reading immediately after writing may require awaiting the write task fully.
*   **Null Handling**: `LoadLatestSnapshotAsync` explicitly returns `null` when no snapshot exists. Callers must handle this case gracefully to avoid `NullReferenceException` when accessing snapshot properties.
*   **Destructive Operations**: Methods such as `DeleteAllEventsAsync` and `DeleteAllSnapshotsAsync` are irreversible. Implementations should ensure these are only invoked during specific lifecycle events (e.g., actor termination or administrative cleanup) to prevent accidental data loss.
*   **Empty Collections**: If no events match the criteria in `ReadEventsAsync` or `ReadEventsBackwardAsync`, an empty `IEnumerable<ActorEvent>` is returned rather than `null`.
