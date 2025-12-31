# InMemorySnapshotStore

A lightweight in-memory implementation of `ISnapshotStore` that keeps actor snapshots in volatile memory. It is primarily intended for testing and development scenarios where persistence is not required, or as a fallback store when no persistent store is configured.

## API

### `SaveSnapshotAsync`

Persists the provided actor snapshot to the in-memory store.

- **Parameters**
  - `snapshot` (`ActorSnapshot`): The snapshot to save. Must not be `null`.
- **Return value**
  - A `Task` that completes when the snapshot has been stored.
- **Exceptions**
  - Throws `ArgumentNullException` if `snapshot` is `null`.

### `LoadLatestSnapshotAsync`

Retrieves the most recent snapshot for the actor, if one exists.

- **Parameters**
  - (None)
- **Return value**
  - A `Task<ActorSnapshot?>` that resolves to the latest snapshot if found, or `null` if no snapshot exists.
- **Exceptions**
  - (None)

### `DeleteSnapshotsAsync`

Removes snapshots older than the specified sequence number, retaining only the most recent ones.

- **Parameters**
  - `maxSequenceNumber` (`long`): The maximum sequence number to retain. Snapshots with a sequence number less than or equal to this value will be removed.
- **Return value**
  - A `Task` that completes when the deletions have been processed.
- **Exceptions**
  - (None)

### `DeleteAllSnapshotsAsync`

Removes all snapshots stored for the actor.

- **Parameters**
  - (None)
- **Return value**
  - A `Task` that completes when all snapshots have been removed.
- **Exceptions**
  - (None)

## Usage
