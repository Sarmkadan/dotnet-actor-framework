# InMemoryEventJournal

The `InMemoryEventJournal` is a transient, in-memory implementation of an event journal used within the actor framework. It provides a lightweight storage mechanism for actor events during runtime, enabling event sourcing patterns without persistence. This type is primarily intended for testing, development, or scenarios where durability is not required.

## API

### `Task AppendEventsAsync`
Appends one or more events to the journal.

**Parameters:**
- `events` (`IEnumerable<ActorEvent>`): The collection of events to append. Must not be `null`.

**Returns:**
A `Task` representing the asynchronous operation.

**Throws:**
- `ArgumentNullException`: Thrown if `events` is `null`.

---

### `Task<IEnumerable<ActorEvent>> ReadEventsAsync`
Reads events from the journal in chronological order (oldest to newest).

**Parameters:**
None.

**Returns:**
A `Task` resolving to an enumerable of `ActorEvent` instances in the order they were appended.

**Throws:**
None.

---

### `Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync`
Reads events from the journal in reverse chronological order (newest to oldest).

**Parameters:**
None.

**Returns:**
A `Task` resolving to an enumerable of `ActorEvent` instances in reverse order of appending.

**Throws:**
None.

---

### `Task DeleteEventsAsync`
Deletes a subset of events from the journal based on unspecified criteria (implementation-dependent).

**Parameters:**
None.

**Returns:**
A `Task` representing the asynchronous operation.

**Throws:**
None.

---

### `Task DeleteAllEventsAsync`
Clears all events from the journal.

**Parameters:**
None.

**Returns:**
A `Task` representing the asynchronous operation.

**Throws:**
None.

## Usage

### Example 1: Appending and Reading Events
