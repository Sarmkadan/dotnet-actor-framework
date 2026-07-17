# InMemoryEventJournalExtensions

Provides extension methods for working with an in-memory event journal implementation of the `IEventJournal` interface. These methods simplify common event sourcing operations like appending, reading, counting, and querying events without requiring direct interaction with the underlying storage mechanism.

## API

### `AppendEventAsync`

Appends a single event to the in-memory event journal.

- **Parameters**
  - `journal` – The `IEventJournal` instance to append to.
  - `event` – The `ActorEvent` to append.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task` that completes when the event has been appended.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` or `event` is `null`.

---

### `ReadAllEventsAsync`

Reads all events from the in-memory event journal in sequence.

- **Parameters**
  - `journal` – The `IEventJournal` to read from.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task<IEnumerable<ActorEvent>>` containing all events in order.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` is `null`.

---

### `CountEventsAsync`

Counts the total number of events in the in-memory event journal.

- **Parameters**
  - `journal` – The `IEventJournal` to count events in.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task<long>` with the total event count.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` is `null`.

---

### `HasEventsAsync`

Checks whether the in-memory event journal contains any events.

- **Parameters**
  - `journal` – The `IEventJournal` to check.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task<bool>` that is `true` if the journal contains at least one event, otherwise `false`.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` is `null`.

---
### `GetFirstEventAsync`

Retrieves the first event in the in-memory event journal.

- **Parameters**
  - `journal` – The `IEventJournal` to query.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task<ActorEvent?>` with the first event, or `null` if the journal is empty.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` is `null`.

---
### `GetLastEventAsync`

Retrieves the last event in the in-memory event journal.

- **Parameters**
  - `journal` – The `IEventJournal` to query.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task<ActorEvent?>` with the last event, or `null` if the journal is empty.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` is `null`.

---
### `GetEventAtSequenceAsync`

Retrieves the event at a specific sequence number in the in-memory event journal.

- **Parameters**
  - `journal` – The `IEventJournal` to query.
  - `sequenceNumber` – The sequence number of the desired event.
  - `cancellationToken` – Optional token to monitor for cancellation.
- **Return value**
  Returns a `Task<ActorEvent?>` with the event at the specified sequence number, or `null` if no such event exists.
- **Exceptions**
  Throws `ArgumentNullException` if `journal` is `null`.
  Throws `ArgumentOutOfRangeException` if `sequenceNumber` is negative.

## Usage

### Appending and reading events

```csharp
using var system = new ActorSystemBuilder()
    .WithInMemoryJournal()
    .Build();

var journal = system.GetEventJournal();

// Append an event
await InMemoryEventJournalExtensions.AppendEventAsync(journal, new ActorEvent("UserCreated", "user-123"));

// Read all events
var events = await InMemoryEventJournalExtensions.ReadAllEventsAsync(journal);
foreach (var e in events)
{
    Console.WriteLine($"Event: {e.Type} - {e.Payload}");
}
```

### Querying specific events

```csharp
using var system = new ActorSystemBuilder()
    .WithInMemoryJournal()
    .Build();

var journal = system.GetEventJournal();

// Append multiple events
await InMemoryEventJournalExtensions.AppendEventAsync(journal, new ActorEvent("OrderPlaced", "order-456"));
await InMemoryEventJournalExtensions.AppendEventAsync(journal, new ActorEvent("PaymentProcessed", "order-456"));

// Check if events exist
bool hasEvents = await InMemoryEventJournalExtensions.HasEventsAsync(journal); // true

// Get the last event
var lastEvent = await InMemoryEventJournalExtensions.GetLastEventAsync(journal);
Console.WriteLine($"Last event: {lastEvent?.Type}");

// Get event by sequence number
var firstEvent = await InMemoryEventJournalExtensions.GetEventAtSequenceAsync(journal, 0);
Console.WriteLine($"First event: {firstEvent?.Type}");
```

## Notes

- All methods are thread-safe and may be called concurrently from multiple threads without additional synchronization.
- The in-memory journal is ephemeral and exists only for the lifetime of the `ActorSystem`; events are not persisted across restarts.
- Sequence numbers are zero-based and assigned in the order events are appended; gaps do not occur unless events are explicitly removed.
- Cancellation is cooperative; if the `cancellationToken` is triggered, the operation may complete in a faulted state with an `OperationCanceledException`.
