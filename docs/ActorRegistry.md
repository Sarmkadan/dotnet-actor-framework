# ActorRegistry

Central registry for managing and resolving actor references within the actor system. Provides methods to register, unregister, and query actors by path, ID, or hierarchical relationships.

## API

### `Register(ActorRef actor)`

Registers an actor reference in the registry. The actor becomes discoverable via path-based lookups and participates in hierarchical queries.

- **Parameters**
  - `actor`: The `ActorRef` to register. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `actor` is `null`.
  - `InvalidOperationException`: If an actor with the same path or ID already exists.

### `Unregister(ActorRef actor)`

Removes an actor reference from the registry. The actor is no longer discoverable via path-based or ID-based lookups.

- **Parameters**
  - `actor`: The `ActorRef` to unregister. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `actor` is `null`.
  - `KeyNotFoundException`: If the actor is not registered.

### `ActorRef? GetByPath(string path)`

Retrieves an actor reference by its hierarchical path (e.g., `/user/parent/child`).

- **Parameters**
  - `path`: The path string to resolve.
- **Returns**
  - The `ActorRef` if found; otherwise, `null`.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `ActorRef? GetById(Guid id)`

Retrieves an actor reference by its unique identifier.

- **Parameters**
  - `id`: The unique identifier of the actor.
- **Returns**
  - The `ActorRef` if found; otherwise, `null`.
- **Throws**
  - `ArgumentNullException`: If `id` is `default` (empty GUID).

### `IReadOnlyList<ActorRef> GetChildren(ActorRef parent)`

Returns all direct child actors of the specified parent actor.

- **Parameters**
  - `parent`: The parent `ActorRef`. Must not be `null`.
- **Returns**
  - A read-only list of child actors. Empty if no children exist.
- **Throws**
  - `ArgumentNullException`: If `parent` is `null`.
  - `KeyNotFoundException`: If the parent is not registered.

### `IReadOnlyList<ActorRef> GetDescendants(ActorRef parent)`

Returns all actors in the subtree rooted at the specified parent actor, including the parent itself.

- **Parameters**
  - `parent`: The root `ActorRef` of the subtree. Must not be `null`.
- **Returns**
  - A read-only list of descendant actors. Empty if no descendants exist.
- **Throws**
  - `ArgumentNullException`: If `parent` is `null`.
  - `KeyNotFoundException`: If the parent is not registered.

### `IReadOnlyList<ActorRef> GetAll()`

Returns all registered actors in the registry.

- **Returns**
  - A read-only list of all `ActorRef` instances. Never `null`.

### `bool Contains(ActorRef actor)`

Determines whether the specified actor is registered.

- **Parameters**
  - `actor`: The `ActorRef` to check. Must not be `null`.
- **Returns**
  - `true` if the actor is registered; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `actor` is `null`.

### `int GetCount()`

Returns the total number of registered actors.

- **Returns**
  - The count of registered actors.

### `void Clear()`

Removes all registered actors from the registry.

## Usage

### Example 1: Basic Registration and Lookup
