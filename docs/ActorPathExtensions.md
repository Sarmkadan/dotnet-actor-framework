# ActorPathExtensions

Provides extension methods for working with `ActorPath` instances, including path manipulation, validation, and hierarchical relationship checks.

## API

### `ActorPath? GetParent(ActorPath path)`

Gets the parent path of the given `ActorPath`.

- **Parameters**
  - `path`: The `ActorPath` instance to get the parent of.
- **Returns**
  - The parent `ActorPath` if the path has a parent; otherwise, `null`.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `string GetName(ActorPath path)`

Gets the name segment of the given `ActorPath`.

- **Parameters**
  - `path`: The `ActorPath` instance to extract the name from.
- **Returns**
  - The name segment as a string.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `int GetDepth(ActorPath path)`

Calculates the depth of the given `ActorPath` in its hierarchy.

- **Parameters**
  - `path`: The `ActorPath` instance to measure.
- **Returns**
  - The number of segments in the path, including the root.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `bool IsChildOf(ActorPath child, ActorPath parent)`

Determines whether the given `child` path is a direct or indirect child of the `parent` path.

- **Parameters**
  - `child`: The child path to check.
  - `parent`: The parent path to compare against.
- **Returns**
  - `true` if `child` is a child of `parent`; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If either `child` or `parent` is `null`.

### `string? GetRelativePath(ActorPath path, ActorPath ancestor)`

Computes the relative path from the given `path` to its `ancestor`.

- **Parameters**
  - `path`: The path to compute the relative path from.
  - `ancestor`: The ancestor path to compute the relative path to.
- **Returns**
  - The relative path as a string if `ancestor` is a valid ancestor of `path`; otherwise, `null`.
- **Throws**
  - `ArgumentNullException`: If either `path` or `ancestor` is `null`.

### `bool IsValidPath(string path)`

Validates whether the given string is a valid `ActorPath`.

- **Parameters**
  - `path`: The string to validate.
- **Returns**
  - `true` if the string is a valid path; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

### `ActorPath CreateChild(ActorPath parent, string name)`

Creates a new child path under the given parent path with the specified name.

- **Parameters**
  - `parent`: The parent path.
  - `name`: The name of the new child segment.
- **Returns**
  - A new `ActorPath` representing the child.
- **Throws**
  - `ArgumentNullException`: If either `parent` or `name` is `null`.
  - `ArgumentException`: If `name` is empty or contains invalid characters.

### `IEnumerable<ActorPath> GetAncestors(ActorPath path)`

Enumerates all ancestor paths of the given `ActorPath`, starting from the immediate parent up to the root.

- **Parameters**
  - `path`: The path to enumerate ancestors for.
- **Returns**
  - An enumerable of `ActorPath` instances representing the ancestors.
- **Throws**
  - `ArgumentNullException`: If `path` is `null`.

## Usage
