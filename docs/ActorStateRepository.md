# ActorStateRepository

Provides asynchronous persistence and retrieval of an actor's state within the dotnet-actor-framework. The repository ties state operations to a specific actor identifier and path, exposing both the current in‑memory state and metadata about the last persisted snapshot.

## API

### ActorStateRepository(Guid actorId, ActorPath actorPath)
Initializes a new repository for the given actor.

- **Purpose**: Prepares the repository to perform state operations for the actor identified by `actorId` and located at `actorPath`.
- **Parameters**:
  - `actorId`: The unique identifier of the actor.
  - `actorPath`: The hierarchical path of the actor; must not be `null`.
- **Return**: A new `ActorStateRepository` instance.
- **Throws**:
  - `ArgumentNullException` if `actorPath` is `null`.

### SaveStateAsync(object state, CancellationToken cancellationToken = default)
Asynchronously persists the supplied state.

- **Purpose**: Writes `state` to the underlying storage so it can be recovered later.
- **Parameters**:
  - `state`: The object representing the actor's current state.
  - `cancellationToken`: Optional token to observe cancellation requests.
- **Return**: `Task<bool>` – `true` if the state was successfully saved; `false` if the save operation was ignored (e.g., no change detected).
- **Throws**:
  - `OperationCanceledException` if the token is canceled.
  - `IOException` or derived storage exceptions for I/O failures.
  - `ArgumentNullException` if `state` is `null`.

### LoadStateAsync(CancellationToken cancellationToken = default)
Asynchronously loads the persisted state as a dictionary.

- **Purpose**: Retrieves the actor's state from storage and returns it as a mutable dictionary of property names to values.
- **Parameters**:
  - `cancellationToken`: Optional token to observe cancellation requests.
- **Return**: `Task<Dictionary<string, object>?>` – a dictionary containing the state, or `null` if no state has been persisted.
- **Throws**:
  - `OperationCanceledException` if the token is canceled.
  - `IOException` or derived storage exceptions for I/O failures.

### DeleteStateAsync(CancellationToken cancellationToken = default)
Asynchronously removes any persisted state for the actor.

- **Purpose**: Deletes the stored state so that subsequent load operations return `null`.
- **Parameters**:
  - `cancellationToken`: Optional token to observe cancellation requests.
- **Return**: `Task<bool>` – `true` if state was deleted; `false` if no state existed to delete.
- **Throws**:
  - `OperationCanceledException` if the token is canceled.
  - `IOException` or derived storage exceptions for I/O failures.

### GetSnapshotAsync(CancellationToken cancellationToken = default)
Asynchronously obtains a snapshot of the actor's state.

- **Purpose**: Returns an immutable `ActorStateSnapshot` representing the current persisted state, useful for debugging or checkpointing.
- **Parameters**:
  - `cancellationToken`: Optional token to observe cancellation requests.
- **Return**: `Task<ActorStateSnapshot?>` – a snapshot if state exists; otherwise `null`.
- **Throws**:
  - `OperationCanceledException` if the token is canceled.
  - `IOException` or derived storage exceptions for I/O failures.

### HasState(CancellationToken cancellationToken = default)
Asynchronously checks whether any state is persisted for the actor.

- **Purpose**: Determines if a save operation has previously succeeded for this actor.
- **Parameters**:
  - `cancellationToken`: Optional token to observe cancellation requests.
- **Return**: `Task<bool>` – `true` if state exists; `false` otherwise.
- **Throws**:
  - `OperationCanceledException` if the token is canceled.
  - `IOException` or derived storage exceptions for I/O failures.

### ActorId
- **Purpose**: Gets the unique identifier of the actor associated with this repository.
- **Property Value**: `Guid`

### ActorPath
- **Purpose**: Gets the hierarchical path of the actor.
- **Property Value**: `ActorPath`

### State
- **Purpose**: Gets the current in‑memory state object last assigned via `SaveStateAsync` or set externally.
- **Property Value**: `object`

### SavedAt
- **Purpose**: Gets the UTC timestamp when the state was last successfully persisted.
- **Property Value**: `DateTime`

### SequenceNr
- **Purpose**: Gets the monotonically increasing sequence number that increments on each successful state save.
- **Property Value**: `long`

### Version
- **Purpose**: Gets the schema version of the persisted state format.
- **Property Value**: `int`

### ActorStateSnapshot
- **Purpose**: Gets a snapshot object representing the current persisted state; equivalent to the result of `GetSnapshotAsync` when called synchronously (if implemented).
- **Property Value**: `ActorStateSnapshot`

## Usage

### Example 1: Saving and loading state
```csharp
var repo = new ActorStateRepository(
    actorId: Guid.NewGuid(),
    actorPath: ActorPath.Root / "users" / "alice");

// Assume we have some state to persist.
var state = new { Name = "Alice", Age = 30, IsActive = true };

// Persist the state.
bool saved = await repo.SaveStateAsync(state);
if (!saved)
{
    // Handle failure (e.g., log, retry).
}

// Later, retrieve the state.
Dictionary<string, object>? loaded = await repo.LoadStateAsync();
if (loaded != null)
{
    Console.WriteLine($"Loaded Name: {loaded["Name"]}");
}
```

### Example 2: Checking state existence, snapshotting, and cleaning up
```csharp
var repo = new ActorStateRepository(
    actorId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
    actorPath: ActorPath.Root / "orders" / "order-42");

// Does any state exist?
bool has = await repo.HasState();
if (has)
{
    // Obtain a snapshot for audit.
    ActorStateSnapshot? snap = await repo.GetSnapshotAsync();
    if (snap != null)
    {
        Console.WriteLine($"Snapshot version: {snap.Version}, saved at: {snap.SavedAt}");
    }

    // Remove state when no longer needed.
    bool deleted = await repo.DeleteStateAsync();
    if (!deleted)
    {
        // Unexpected; maybe another process deleted it concurrently.
    }
}
```

## Notes
- The repository does **not** synchronize access; concurrent calls to any of its methods from multiple threads are allowed but may produce race conditions. External synchronization is required if strict ordering of operations is needed.
- `State`, `SavedAt`, `SequenceNr`, and `Version` reflect the values from the **most recent successful** `SaveStateAsync` call. They are not updated automatically by `LoadStateAsync` or `GetSnapshotAsync`.
- `HasState` may return `true` immediately after a `SaveStateAsync` that has not yet persisted to storage due to asynchronous I/O; callers should treat the result as a best‑effort indicator.
- If `SaveStateAsync` throws, the repository’s internal metadata (`SavedAt`, `SequenceNr`, `Version`, `State`) is left unchanged.
- `DeleteStateAsync` returning `false` indicates that no state was present at the moment of the check; a concurrent `SaveStateAsync` could still succeed afterward, leading to a state being recreated after a delete attempt.
- `ActorStateSnapshot` is an immutable copy of the persisted state at the time of the snapshot; modifications to the returned object do not affect the repository’s stored state.
