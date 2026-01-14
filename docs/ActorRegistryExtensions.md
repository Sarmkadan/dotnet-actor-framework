# ActorRegistryExtensions

Provides extension methods for the `ActorRegistry` type to simplify common actor lookup operations in the actor framework.

## API

### `Get(IActorRegistry registry, string path)`

Retrieves the actor reference associated with the given path from the registry.

- **Parameters**
  - `registry`: The `IActorRegistry` instance to query.
  - `path`: The unique path of the actor to locate.
- **Return value**
  - An `ActorRef` instance if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `registry` or `path` is `null`.

---

### `Get(IActorRegistry registry, Type actorType)`

Retrieves the first actor reference of the specified type from the registry.

- **Parameters**
  - `registry`: The `IActorRegistry` instance to query.
  - `actorType`: The `Type` of the actor to locate.
- **Return value**
  - An `ActorRef` instance if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `registry` or `actorType` is `null`.

---
### `FindAll(IActorRegistry registry, Type actorType)`

Finds all actor references of the specified type in the registry.

- **Parameters**
  - `registry`: The `IActorRegistry` instance to query.
  - `actorType`: The `Type` of the actors to locate.
- **Return value**
  - An `IReadOnlyList<ActorRef>` containing all matching actor references. The list is empty if no matches are found.
- **Exceptions**
  - Throws `ArgumentNullException` if `registry` or `actorType` is `null`.

---
### `GetRoot(IActorRegistry registry)`

Retrieves the root actor reference from the registry.

- **Parameters**
  - `registry`: The `IActorRegistry` instance to query.
- **Return value**
  - An `ActorRef` instance representing the root actor if it exists; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `registry` is `null`.

## Usage
