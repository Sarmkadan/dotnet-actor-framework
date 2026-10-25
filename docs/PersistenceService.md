# PersistenceService

`PersistenceService` (`src/DotNetActorFramework/Persistence/PersistenceService.cs`, namespace `DotNetActorFramework.Persistence`) is a facade over two storage contracts: snapshot storage (`ISnapshotStore`) and event journaling (`IEventJournal`). It gives actors one entry point to save and load snapshots, append and read events, and delete either. It also applies a simple auto-snapshot policy: after a configurable number of appended events for an actor, it writes a snapshot.

The class holds no storage of its own. Every call is forwarded to the injected `ISnapshotStore` or `IEventJournal`, so the storage technology is decided by whichever implementations are registered.

## Purpose

- Recover actor state quickly by loading the latest snapshot and replaying only the events that follow it.
- Record state changes as events (event sourcing) through `AppendEventsAsync`.
- Keep the snapshot count low by saving snapshots periodically, instead of relying on the full event log for every recovery.

## Backends

`PersistenceService` is backend-agnostic. It depends only on the two interfaces below.

| Contract | Namespace | Role |
|----------|-----------|------|
| `ISnapshotStore` | `DotNetActorFramework.Persistence.Abstractions` | Save, load latest, and delete snapshots |
| `IEventJournal` | `DotNetActorFramework.Persistence.Abstractions` | Append, read forward or backward, and delete events |

Backends in this repository:

- **In-memory** (`InMemorySnapshotStore`, `InMemoryEventJournal`), namespace `DotNetActorFramework.Persistence.InMemory`. This is the default: `ActorSystemOptions.DefaultPersistenceBackend` is `PersistenceBackend.InMemory`, and the DI setup registers both implementations as singletons. Data is kept in process memory and is lost on restart. `InMemorySnapshotStore` keeps only the most recent snapshot per actor.
- **Resilient decorator** (`ResilientEventJournal`), registered with `AddResilientEventJournal` or `AddResilientEventJournalProduction`. It wraps an existing `IEventJournal` with retry and circuit-breaker behavior. It is a decorator, not a storage backend.

Other values of `PersistenceBackend` exist, but they are not implemented. Selecting `File`, `LiteDb`, or `PostgreSql` makes the DI setup throw `NotImplementedException` at registration time.

To use a custom store, register your own `ISnapshotStore` and `IEventJournal` implementations. `PersistenceService` is then resolved with those dependencies.

## Constructor

### `public PersistenceService(ISnapshotStore snapshotStore, IEventJournal eventJournal, ILogger<PersistenceService>? logger = null, int snapshotIntervalEvents = 100)`

- **`snapshotStore`**: the snapshot store. Must not be `null`; otherwise `ArgumentNullException` is thrown.
- **`eventJournal`**: the event journal. Must not be `null`; otherwise `ArgumentNullException` is thrown.
- **`logger`**: optional logger. When `null`, no logging is done.
- **`snapshotIntervalEvents`**: number of events after which an automatic snapshot is written. Default is `100`.

## Methods

All methods are `async` and return a `Task`. Each takes `Guid actorId` and `ActorPath actorPath` to identify the actor. The path is converted to a string (`actorPath.ToString()`) before it is passed to the storage contracts.

### Snapshot operations

#### `Task SaveSnapshotAsync(Guid actorId, ActorPath actorPath, object state, long sequenceNr)`
Builds an `ActorSnapshot` with the current UTC time as its timestamp and passes it to `ISnapshotStore.SaveSnapshotAsync`. Logs and rethrows any exception.

#### `Task<ActorSnapshot?> LoadLatestSnapshotAsync(Guid actorId, ActorPath actorPath)`
Returns the most recent snapshot for the actor, or `null` if none exists.

#### `Task DeleteSnapshotsAsync(Guid actorId, ActorPath actorPath, long maxSequenceNr)`
Deletes the actor's snapshots with a sequence number up to and including `maxSequenceNr`.

#### `Task DeleteAllSnapshotsAsync(Guid actorId, ActorPath actorPath)`
Deletes all snapshots for the actor.

### Event journal operations

#### `Task AppendEventsAsync(Guid actorId, ActorPath actorPath, IEnumerable<ActorEvent> events)`
Appends the events to the journal, then updates a per-actor counter of appended events. When the counter reaches `snapshotIntervalEvents`, the method saves a snapshot and resets the counter to `0`. See [Auto-snapshot](#auto-snapshot) below.

#### `Task<IEnumerable<ActorEvent>> ReadEventsAsync(Guid actorId, ActorPath actorPath, long fromSequenceNr, long toSequenceNr)`
Reads events in forward order. Both bounds are inclusive.

#### `Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync(Guid actorId, ActorPath actorPath, long fromSequenceNr, long toSequenceNr)`
Reads events in reverse order. Both bounds are inclusive.

#### `Task DeleteEventsAsync(Guid actorId, ActorPath actorPath, long maxSequenceNr)`
Deletes the actor's events with a sequence number up to and including `maxSequenceNr`.

#### `Task DeleteAllEventsAsync(Guid actorId, ActorPath actorPath)`
Deletes all events for the actor.

## Data types

These records live in `DotNetActorFramework.Persistence.Abstractions`.

```csharp
public record ActorSnapshot(Guid ActorId, string ActorPath, object State, long SequenceNr, DateTime Timestamp);

public record ActorEvent(Guid ActorId, string ActorPath, long SequenceNr, DateTime Timestamp, object EventData);
```

`ActorPath` on both records is the string form of the actor path. `PersistenceService` fills it in from the `ActorPath` argument.

## Auto-snapshot

The counter used for the auto-snapshot policy is kept per actor, keyed by `{actorId}_{actorPath}`.

1. `AppendEventsAsync` appends the events to the journal.
2. The counter for the actor is increased by the number of events appended.
3. If the counter is at or above `snapshotIntervalEvents`, `SaveSnapshotAsync` is called with `state = null` and `sequenceNr = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`. The counter is then reset to `0`.

Because the snapshot is saved with a `null` state, it does not capture actor state. It is not usable for recovery. The sequence number is a millisecond timestamp, not the journal's event sequence number.

The counter lives only in memory. It starts at zero again after a process restart.

## Usage

### Example 1: Recover an actor from snapshot and events

```csharp
public async Task<long> RecoverAsync(Guid actorId, ActorPath actorPath, PersistenceService persistence)
{
    var snapshot = await persistence.LoadLatestSnapshotAsync(actorId, actorPath);
    long fromSequenceNr = 0;

    if (snapshot is not null)
    {
        ApplySnapshot(snapshot.State);
        fromSequenceNr = snapshot.SequenceNr + 1;
    }

    var events = await persistence.ReadEventsAsync(actorId, actorPath, fromSequenceNr, long.MaxValue);
    foreach (var evt in events)
    {
        ApplyEvent(evt.EventData);
    }

    return fromSequenceNr;
}
```

`ApplySnapshot` and `ApplyEvent` stand in for the actor's own state logic.

### Example 2: Append events

```csharp
public async Task RecordAsync(Guid actorId, ActorPath actorPath, PersistenceService persistence, object evt)
{
    var record = new ActorEvent(actorId, actorPath.ToString(), 0, DateTime.UtcNow, evt);
    await persistence.AppendEventsAsync(actorId, actorPath, new[] { record });
}
```

The `SequenceNr` value is set by the caller. `PersistenceService` does not assign sequence numbers to events.

## Notes

- **Auto-snapshot stores no state.** The auto-snapshot described above saves `null` as the state. Treat it as a placeholder until the framework passes the real actor state into the policy.
- **Pruning is not done here.** The class summary says the auto-snapshot policy "prunes older snapshots". The code does not do this. `PersistenceService` never calls `DeleteSnapshotsAsync`. Pruning happens only inside `InMemorySnapshotStore`, which keeps only the highest-sequence snapshot per actor.
- **Counter is per process.** The auto-snapshot counter is in memory and per actor. Across multiple processes or restarts, snapshots happen at a different rate than the configured interval.
- **Events are enumerated more than once.** `AppendEventsAsync` calls `Count()` on `events` for logging and for the counter. Pass a materialized collection (for example, an array or `List<T>`) to avoid enumerating a lazy sequence twice.
- **Concurrency.** `PersistenceService` keeps no per-call state beyond the counter dictionary, which is a `ConcurrentDictionary`. Concurrency control for appends depends on the `IEventJournal` implementation.
- **Logging.** The `ILogger<PersistenceService>` is optional. Without it, no output is produced. `SaveSnapshotAsync` logs its errors and rethrows them.
