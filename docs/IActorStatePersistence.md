# IActorStatePersistence

The `IActorStatePersistence` interface defines a standardized contract for managing the persistence of actor state within the `dotnet-actor-framework`. It abstracts the underlying storage mechanism, enabling actors to reliably save, load, delete, and verify their state data across different persistence providers, such as file-based systems or remote databases.

## API

### Interface Methods (`IActorStatePersistence`)

*   **`Task SaveAsync`**
    Saves the actor's state data to the configured persistence store.
    *   *Exceptions*: May throw `IOException` if writing to the storage fails.

*   **`Task<object?> LoadAsync`**
    Retrieves the persisted state data for the actor.
    *   *Returns*: The deserialized state object, or `null` if no state is found.

*   **`Task DeleteAsync`**
    Removes the actor's persisted state from the storage.
    *   *Exceptions*: May throw `IOException` if the deletion operation fails.

*   **`Task<bool> ExistsAsync`**
    Checks whether state data currently exists for the specified actor.
    *   *Returns*: `true` if state exists; otherwise, `false`.

### Implementation Class (`FileActorStatePersistence`)

The `FileActorStatePersistence` class provides a concrete implementation of state storage using the file system.

*   **`Guid ActorId`**: The unique identifier of the actor whose state is being managed.
*   **`string ActorPath`**: The file system path where the state data is stored.
*   **`byte[] StateData`**: The raw serialized bytes of the actor's state.
*   **`DateTime CreatedAt`**: The timestamp indicating when the state was initially persisted.
*   **`long Version`**: The version number of the state data, used for concurrency control and tracking updates.

## Usage

### Example 1: Persisting Actor State

This example demonstrates how an actor service utilizes an `IActorStatePersistence` implementation to save its current state.

```csharp
public async Task UpdateActorState(IActorStatePersistence persistence, object newState)
{
    // The implementation handles the serialization and persistence
    await persistence.SaveAsync();
}
```

### Example 2: Loading and Verifying Actor State

This example illustrates checking for the existence of state before attempting to load it to avoid unnecessary operations.

```csharp
public async Task<object?> GetActorState(IActorStatePersistence persistence)
{
    if (await persistence.ExistsAsync())
    {
        return await persistence.LoadAsync();
    }
    
    return null; // Return default or handle as initial state
}
```

## Notes

*   **Thread Safety**: Implementations of `IActorStatePersistence` are responsible for ensuring thread safety during `SaveAsync`, `LoadAsync`, and `DeleteAsync` operations, particularly when accessing shared storage resources.
*   **Concurrency**: The `Version` property in `FileActorStatePersistence` should be utilized by implementers to prevent lost updates in concurrent environments (optimistic concurrency control).
*   **Data Integrity**: If an operation fails during `SaveAsync`, the state may be left in an inconsistent state. It is recommended to use atomic file operations or temporary file staging within the implementation.
*   **Null Handling**: `LoadAsync` returns `null` when no state exists; consuming code must handle this appropriately to initialize default actor state.
