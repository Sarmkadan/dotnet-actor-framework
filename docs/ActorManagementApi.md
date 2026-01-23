# ActorManagementApi

The `ActorManagementApi` class serves as both a client for actor management operations and a container for the results of those operations. It exposes methods to query, list, and terminate actors, while its properties reflect the outcome of the most recent call (e.g., success status, message, and the returned data). This design allows callers to perform an action and then inspect the result on the same instance without needing separate response objects.

## API

### Constructor

- **`public ActorManagementApi()`**  
  Initializes a new instance of the class. All properties are set to their default values (e.g., `Success` is `false`, `Message` is empty, `Actors` is an empty list).

### Properties

- **`public bool Success`**  
  Indicates whether the last operation completed successfully. `true` if successful; otherwise `false`.

- **`public string Message`**  
  A human-readable message describing the outcome of the last operation. Typically contains an error description when `Success` is `false`.

- **`public object? Data`**  
  Optional payload returned by the last operation. The type depends on the operation (e.g., an `ActorInfo` object after a successful `GetActor` call).

- **`public string Path`**  
  The URI or logical path used in the last operation, if applicable.

- **`public Guid Id`**  
  The identifier of the actor that was the subject of the last operation (e.g., after a `GetActor` or `TerminateActorAsync` call).

- **`public bool IsAlive`**  
  Indicates whether the actor identified by `Id` is currently alive. Relevant after operations that query actor state.

- **`public DateTime CreatedAt`**  
  The creation timestamp of the actor identified by `Id`, if available.

- **`public List<ActorInfo> Actors`**  
  The list of actors returned by a list operation (e.g., `ListActors`, `ListActorsByParent`, `GetErrorActors`). May be empty if no actors match.

- **`public int Total`**  
  The total number of actors matching the query criteria, independent of pagination.

- **`public int Limit`**  
  The maximum number of actors returned in the current page (pagination limit).

- **`public int Offset`**  
  The zero-based offset used for pagination in the last list operation.

### Methods

- **`public ActorInfo? GetActor()`**  
  Retrieves a single actor by its identifier.  
  **Parameters:** Accepts an actor identifier (typically a `Guid` or string).  
  **Returns:** The matching `ActorInfo` object, or `null` if no actor with that identifier exists.  
  **Throws:** `InvalidOperationException` if the identifier is invalid or the operation fails due to connectivity issues.

- **`public ActorListResponse ListActors()`**  
  Returns a paginated list of all actors.  
  **Parameters:** Accepts optional pagination parameters (`limit`, `offset`) and optional filter criteria.  
  **Returns:** An `ActorListResponse` containing the matching actors and pagination metadata.  
  **Throws:** `InvalidOperationException` on invalid parameters or underlying service errors.

- **`public ActorListResponse ListActorsByParent()`**  
  Returns a paginated list of actors that are children of a specified parent actor.  
  **Parameters:** Accepts the parent actor identifier, plus optional pagination and filter parameters.  
  **Returns:** An `ActorListResponse` with the child actors.  
  **Throws:** `ArgumentException` if the parent identifier is null or empty; `InvalidOperationException` on service failure.

- **`public async Task<ApiResponse> TerminateActorAsync()`**  
  Asynchronously terminates the actor identified by the provided identifier.  
  **Parameters:** Accepts the actor identifier to terminate.  
  **Returns:** An `ApiResponse` indicating the outcome (success/failure and message).  
  **Throws:** `ArgumentNullException` if the identifier is null; `InvalidOperationException` if the actor cannot be found or the termination fails.

- **`public ActorListResponse GetErrorActors()`**  
  Returns a paginated list of actors that are currently in an error state.  
  **Parameters:** Accepts optional pagination parameters.  
  **Returns:** An `ActorListResponse` containing the error-state actors.  
  **Throws:** `InvalidOperationException` on service errors.

- **`public int GetActorCount()`**  
  Returns the total number of actors in the system.  
  **Parameters:** None.  
  **Returns:** An integer count of actors.  
  **Throws:** `InvalidOperationException` if the count cannot be retrieved.

- **`public ActorMetricsSummary? GetActorMetrics()`**  
  Retrieves a summary of actor metrics (e.g., total active, idle, failed counts).  
  **Parameters:** None.  
  **Returns:** An `ActorMetricsSummary` object, or `null` if metrics are unavailable.  
  **Throws:** `InvalidOperationException` on service failure.

## Usage

### Example 1: Listing actors and inspecting results

```csharp
var api = new ActorManagementApi();
var response = api.ListActors(limit: 10, offset: 0);

if (api.Success)
{
    Console.WriteLine($"Found {api.Total} actors, showing {api.Actors.Count}.");
    foreach (var actor in api.Actors)
    {
        Console.WriteLine($"Actor {actor.Id}: {actor.Status}");
    }
}
else
{
    Console.WriteLine($"Error: {api.Message}");
}
```

### Example 2: Terminating an actor asynchronously

```csharp
var api = new ActorManagementApi();
Guid actorId = Guid.Parse("a1b2c3d4-...");

ApiResponse result = await api.TerminateActorAsync(actorId);

if (result.Success)
{
    Console.WriteLine($"Actor {actorId} terminated successfully.");
}
else
{
    Console.WriteLine($"Termination failed: {result.Message}");
}
```

## Notes

- **Edge cases:**  
  - When `ListActors`, `ListActorsByParent`, or `GetErrorActors` returns no results, the `Actors` property is an empty list, `Total` is 0, and `Success` is `true`.  
  - Calling `GetActor` with a non‑existent identifier sets `Success` to `false` and `Data` to `null`.  
  - `TerminateActorAsync` on an already terminated actor may return a successful response or an error depending on the framework’s idempotency guarantees.  
  - Pagination properties (`Limit`, `Offset`, `Total`) are only meaningful after a list operation; they are reset to defaults after other operations.

- **Thread safety:**  
  Instances of `ActorManagementApi` are **not thread‑safe**. Concurrent calls to methods or property reads from multiple threads may produce inconsistent state. Each thread should use its own instance, or external synchronization (e.g., a lock) must be employed when sharing an instance.
