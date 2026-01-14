# ActorDiscoveryService

`ActorDiscoveryService` provides a centralized registry for actor references within a local process, allowing actors to be registered with optional tags and subsequently discovered by other components. It maintains a thread-safe collection of `ActorDiscoveryEntry` records and exposes methods for registration, unregistration, and lookup by tag or full enumeration.

## API

### `public void Register(ActorRef actorRef, params string[] tags)`

Registers an actor reference with the discovery service, optionally associating one or more string tags for later filtered discovery.

- **Parameters:**
  - `actorRef` — The actor reference to register. Must not be null.
  - `tags` — Zero or more string tags to associate with the actor. Duplicate tags for the same registration are ignored.
- **Return value:** None.
- **Exceptions:**
  - `ArgumentNullException` — Thrown when `actorRef` is null.
  - `InvalidOperationException` — Thrown when the same `actorRef` is already registered.

### `public bool Unregister(ActorRef actorRef)`

Removes a previously registered actor reference from the discovery service.

- **Parameters:**
  - `actorRef` — The actor reference to unregister. Must not be null.
- **Return value:** `true` if the actor was found and removed; `false` if the actor was not registered.
- **Exceptions:**
  - `ArgumentNullException` — Thrown when `actorRef` is null.

### `public IReadOnlyList<ActorRef> Discover(string tag)`

Returns all actor references that have been registered with the specified tag.

- **Parameters:**
  - `tag` — The tag to search for. Must not be null or empty.
- **Return value:** A read-only list of `ActorRef` instances matching the tag. Returns an empty list if no actors are registered with the given tag.
- **Exceptions:**
  - `ArgumentNullException` — Thrown when `tag` is null.
  - `ArgumentException` — Thrown when `tag` is an empty string.

### `public IReadOnlyList<ActorRef> DiscoverByTag(string tag)`

Returns all actor references that have been registered with the specified tag. Functionally identical to `Discover`; provided as an alternative naming convention.

- **Parameters:**
  - `tag` — The tag to search for. Must not be null or empty.
- **Return value:** A read-only list of `ActorRef` instances matching the tag. Returns an empty list if no actors are registered with the given tag.
- **Exceptions:**
  - `ArgumentNullException` — Thrown when `tag` is null.
  - `ArgumentException` — Thrown when `tag` is an empty string.

### `public IReadOnlyList<ActorDiscoveryEntry> GetAll()`

Returns a snapshot of all registered entries currently held by the discovery service.

- **Parameters:** None.
- **Return value:** A read-only list of all `ActorDiscoveryEntry` records. Returns an empty list if no actors are registered.
- **Exceptions:** None.

### `public sealed record ActorDiscoveryEntry`

An immutable record representing a single registration in the discovery service.

- **Properties:**
  - `ActorRef ActorRef` — The registered actor reference.
  - `IReadOnlyList<string> Tags` — The tags associated with this actor at registration time.
  - `DateTime RegisteredAt` — The UTC timestamp when the actor was registered.

### `public DateTime RegisteredAt`

Gets the UTC timestamp indicating when the associated `ActorDiscoveryEntry` was created (i.e., when the actor was registered).

## Usage

### Example 1: Registering and discovering actors by tag

```csharp
var discovery = new ActorDiscoveryService();
var worker1 = new ActorRef("worker-1");
var worker2 = new ActorRef("worker-2");

// Register actors with tags describing their roles
discovery.Register(worker1, "worker", "processor");
discovery.Register(worker2, "worker", "validator");

// Later, discover all actors tagged as "worker"
IReadOnlyList<ActorRef> workers = discovery.Discover("worker");
foreach (var worker in workers)
{
    // Send work items to each discovered worker
    Console.WriteLine($"Found worker: {worker.Id}");
}
```

### Example 2: Lifecycle management with GetAll and Unregister

```csharp
var discovery = new ActorDiscoveryService();
var ephemeralActor = new ActorRef("temp-actor");

discovery.Register(ephemeralActor, "ephemeral");

// Inspect all registered entries for monitoring purposes
IReadOnlyList<ActorDiscoveryEntry> allEntries = discovery.GetAll();
foreach (var entry in allEntries)
{
    Console.WriteLine(
        $"Actor: {entry.ActorRef.Id}, " +
        $"Tags: {string.Join(", ", entry.Tags)}, " +
        $"Registered: {entry.RegisteredAt:O}");
}

// When the ephemeral actor terminates, unregister it
bool removed = discovery.Unregister(ephemeralActor);
Console.WriteLine($"Unregistered: {removed}");

// Verify removal
Console.WriteLine($"Remaining entries: {discovery.GetAll().Count}");
```

## Notes

- **Thread safety:** All public methods are safe to call concurrently from multiple threads. The underlying collection is synchronized, and `GetAll` returns a snapshot that is safe to enumerate without holding locks.
- **Duplicate registration:** Attempting to register the same `ActorRef` more than once throws `InvalidOperationException`. Callers must unregister before re-registering the same reference.
- **Tag matching:** Discovery by tag performs an exact, case-sensitive match. Tags are stored as provided at registration time with no normalization.
- **Empty results:** `Discover`, `DiscoverByTag`, and `GetAll` never return null; they return empty read-only lists when no matching entries exist.
- **Snapshot semantics:** The list returned by `GetAll` reflects the state of the registry at the time of the call. Subsequent registrations or unregistrations are not reflected in previously obtained snapshots.
- **Unregister idempotency:** Calling `Unregister` for an actor that is not (or no longer) registered returns `false` and does not throw. This allows safe cleanup without pre-checking registration status.
- **Tag immutability:** The tags associated with an `ActorDiscoveryEntry` are fixed at registration time. To change tags, unregister the actor and register it again with the new tags.
