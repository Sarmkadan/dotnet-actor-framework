# ActorSystem

A lightweight, actor-model runtime component that manages a hierarchy of actors, their lifecycle, and system-level telemetry. It provides discovery, monitoring, and controlled shutdown for a group of cooperating actors.

## API

### `public string Name`
Gets the human-readable name assigned to the actor system when it was created.

### `public Guid Id`
Gets the unique identifier assigned to the actor system at creation time.

### `public DateTime CreatedAt`
Gets the timestamp when the actor system was instantiated.

### `public DateTime? ShutdownAt`
Gets the timestamp when the actor system was shut down, or `null` if it is still running.

### `public bool IsRunning`
Gets a value indicating whether the actor system is currently running and accepting new operations.

### `public ActorSystem`
Constructor that initializes a new actor system with a unique identifier and a human-readable name.

### `public async Task<ActorRef> CreateActorAsync`
Creates a new actor under the root scope and returns a reference to it.

- **Parameters**: none
- **Return value**: `Task<ActorRef>` – a reference to the newly created actor.
- **Exceptions**: Throws if the system is not running or if actor creation fails.

### `public ActorRef? GetActorRef`
Returns a reference to the actor with the specified unique identifier, if it exists and is still alive.

- **Parameters**: none
- **Return value**: `ActorRef?` – the reference, or `null` if no such actor exists.

### `public IReadOnlyList<ActorRef> GetActorsByParent`
Returns a read-only list of references to all actors whose parent is the actor with the specified unique identifier.

- **Parameters**: none
- **Return value**: `IReadOnlyList<ActorRef>` – the list of child actor references.

### `public IReadOnlyList<ActorRef> GetAllActors`
Returns a read-only list of references to all actors currently managed by the system.

- **Parameters**: none
- **Return value**: `IReadOnlyList<ActorRef>` – the list of all actor references.

### `public async Task TerminateActorAsync`
Initiates an orderly shutdown of the actor with the specified unique identifier.

- **Parameters**: none
- **Return value**: `Task` – a task that completes when termination is complete.
- **Exceptions**: Throws if the system is not running or if the actor cannot be found.

### `public int GetActorCount`
Returns the current number of actors managed by the system.

- **Parameters**: none
- **Return value**: `int` – the count of active actors.

### `public IReadOnlyList<ActorRef> GetErrorActors`
Returns a read-only list of references to actors that have encountered an unrecoverable error.

- **Parameters**: none
- **Return value**: `IReadOnlyList<ActorRef>` – the list of errored actor references.

### `public ActorMetricsSummary? GetActorMetricsSummary`
Returns a snapshot of aggregated metrics for all actors in the system, or `null` if metrics are unavailable.

- **Parameters**: none
- **Return value**: `ActorMetricsSummary?` – a summary object containing actor-level statistics, or `null`.

### `public SystemHealthSummary GetHealthSummary`
Returns a snapshot of system-wide health indicators, including actor counts, error states, and resource usage.

- **Parameters**: none
- **Return value**: `SystemHealthSummary` – a summary object containing system-level health data.

### `public async Task ShutdownAsync`
Initiates an orderly shutdown of the entire actor system and all its actors.

- **Parameters**: none
- **Return value**: `Task` – a task that completes when shutdown is complete.
- **Exceptions**: Throws if the system is already shut down or shutting down.

### `public override string ToString`
Returns a string representation of the actor system, including its unique identifier and name.

- **Parameters**: none
- **Return value**: `string` – a human-readable identifier.

### `public Guid SystemId`
Gets the unique identifier assigned to the actor system at creation time.

### `public string SystemName`
Gets the human-readable name assigned to the actor system when it was created.

### `public DateTime CreatedAt`
Gets the timestamp when the actor system was instantiated.

## Usage
