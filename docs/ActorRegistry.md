# ActorRegistry

Central registry for actor registrations, lookups, and hierarchy indexing within the actor system. It keeps three indexes in sync: by `ActorPath`, by actor `Guid`, and by parent path (for child and descendant queries). All public methods are thread-safe: they serialize on a single internal lock.

`ActorRegistry` lives in the `DotNetActorFramework.Services` namespace. It is registered as a singleton by `DependencyInjectionSetup`, so application code normally receives the shared instance through dependency injection rather than creating one.

## API

### `void Register(ActorRef actorRef)`

Adds an actor to the path index and the ID index. If the actor has a parent path, it is also appended to that parent's child list.

- **Parameters**
  - `actorRef`: The `ActorRef` to register. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `actorRef` is `null`.
  - `InvalidOperationException`: If an actor is already registered at the same `Path`.
  - `ActorSystemException`: If any other failure occurs; the original exception is attached as the inner exception.
- **Notes**
  - Only the path is checked for duplicates. Registering a second actor with an ID that is already present silently overwrites the ID index entry.

### `void Unregister(ActorRef actorRef)`

Removes an actor from the path index and the ID index, and removes its path from the parent's child list.

- **Parameters**
  - `actorRef`: The `ActorRef` to unregister. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `actorRef` is `null`.
- **Notes**
  - Unregistering an actor that is not registered is a no-op; no exception is thrown.
  - Children are not removed. They remain registered and keep their entries in the hierarchy index.

### `ActorRef? GetByPath(ActorPath path)`

Looks up an actor by its path.

- **Parameters**
  - `path`: The `ActorPath` to resolve. Must not be `null`.
- **Returns**
  - The registered `ActorRef`, or `null` if no actor is registered at that path.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `ActorRef? GetById(Guid id)`

Looks up an actor by its unique identifier.

- **Parameters**
  - `id`: The actor's `Id`.
- **Returns**
  - The registered `ActorRef`, or `null` if no actor has that ID.

### `IReadOnlyList<ActorRef> GetChildren(ActorPath parentPath)`

Returns the direct children of the actor at `parentPath`.

- **Parameters**
  - `parentPath`: The path of the parent actor. Must not be `null`.
- **Returns**
  - A read-only list of registered direct children. Empty if the parent has no children or is not registered. Never `null`.
- **Throws**
  - `ArgumentNullException`: If `parentPath` is `null`.

### `IReadOnlyList<ActorRef> GetDescendants(ActorPath parentPath)`

Returns every actor below `parentPath` at any depth, walking the hierarchy depth-first.

- **Parameters**
  - `parentPath`: The path of the root of the subtree. Must not be `null`.
- **Returns**
  - A read-only list of all descendants. The parent itself is **not** included. Empty if there are none. Never `null`.
- **Throws**
  - `ArgumentNullException`: If `parentPath` is `null`.

### `IReadOnlyList<ActorRef> GetAll()`

Returns a snapshot of all registered actors.

- **Returns**
  - A read-only list copy of the path index. Never `null`. Later registrations and removals do not affect a list that has already been returned.

### `bool Contains(ActorPath path)`

Checks whether an actor is registered at the given path.

- **Parameters**
  - `path`: The `ActorPath` to check. Must not be `null`.
- **Returns**
  - `true` if an actor is registered at `path`; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `int GetCount()`

Returns the number of registered actors.

- **Returns**
  - The count of entries in the path index.

### `void Clear()`

Removes all registrations from all three indexes.

## Extension methods

`ActorRegistryExtensions` (in the same namespace) adds convenience lookups on top of `ActorRegistry`:

| Method | Description |
|--------|-------------|
| `ActorRef? Get(this ActorRegistry, ActorPath)` | Alias for `GetByPath`. |
| `ActorRef? Get(this ActorRegistry, Guid)` | Alias for `GetById`. |
| `IReadOnlyList<ActorRef> FindAll(this ActorRegistry, Func<ActorRef, bool>)` | Filters all registered actors with a predicate. |
| `ActorRef? GetRoot(this ActorRegistry)` | Returns the first registered actor that has no parent path, or `null`. |

## Usage

### Example: Register, query, and unregister

`ActorRef` has an `internal` constructor, so application code does not create references directly. Instead, the actor system creates them through `ActorSystemConfiguration.CreateActorAsync`, which also registers them in the shared `ActorRegistry`. That method throws `InvalidOperationException` unless `InitializeAsync()` has been called first.

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using Microsoft.Extensions.DependencyInjection;

// Resolve the shared singleton instances from the DI container.
var registry = serviceProvider.GetRequiredService<ActorRegistry>();
var configuration = serviceProvider.GetRequiredService<ActorSystemConfiguration>();

// Initialize the actor system once before creating actors.
await configuration.InitializeAsync();

// Creating actors registers them in the registry.
ActorRef orders = await configuration.CreateActorAsync(ActorPath.Parse("/user/orders"));
ActorRef item = await configuration.CreateActorAsync(ActorPath.Parse("/user/orders/item-1"), orders);

// Lookups by path, ID, and hierarchy.
ActorRef? byPath = registry.GetByPath(ActorPath.Parse("/user/orders/item-1"));
ActorRef? byId = registry.GetById(item.Id);
IReadOnlyList<ActorRef> children = registry.GetChildren(orders.Path);      // contains item
IReadOnlyList<ActorRef> subtree = registry.GetDescendants(orders.Path);    // contains item, not orders

bool exists = registry.Contains(ActorPath.Parse("/user/orders"));          // true
int count = registry.GetCount();

// Removing a single actor leaves its children registered.
registry.Unregister(item);
bool stillThere = registry.Contains(item.Path);                            // false
```

## Related

- [ActorRegistryExtensions](ActorRegistryExtensions.md)
- [ClusterActorRegistry](ClusterActorRegistry.md)
- [ActorPath](ActorPath.md)
- [ActorRef](ActorRef.md)
