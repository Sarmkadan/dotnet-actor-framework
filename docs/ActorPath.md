# ActorPath

Represents an immutable hierarchical path within an actor system. An `ActorPath` identifies a specific actor by its location in the actor tree, analogous to a file system path. It consists of a sequence of segments, a `Name` (the last segment), an optional `Parent`, and a full string representation accessible via the `Path` property. Instances are created either through the constructor or the static `Parse` method, and once constructed the path cannot be modified.

## API

### `public string Path { get; }`

The full string representation of the actor path, e.g., `"/user/myActor/child"`. This is the canonical form used for serialization and display.

### `public string Name { get; }`

The last segment of the path. For a root path this may be an empty string or a special root name depending on the implementation.

### `public ActorPath? Parent { get; }`

The parent path, or `null` if this path represents the root of the actor hierarchy. The parent is derived by removing the last segment.

### `public IReadOnlyList<string> Segments { get; }`

A read-only list of the individual path segments, in order from root to leaf. The list is never `null` but may be empty for a root path.

### `public ActorPath(string path)`

Constructs a new `ActorPath` from the given string representation.  
**Parameters:**  
- `path` – A string representing the actor path (e.g., `"/user/actor1"`).  
**Throws:**  
- `ArgumentNullException` if `path` is `null`.  
- `ArgumentException` if `path` is not a valid actor path format (e.g., empty string, invalid characters).

### `public static ActorPath Parse(string path)`

Parses a string into an `ActorPath` instance. This is equivalent to the constructor but may provide additional validation or caching.  
**Parameters:**  
- `path` – The string to parse.  
**Returns:** A new `ActorPath` representing the given path.  
**Throws:**  
- `ArgumentNullException` if `path` is `null`.  
- `ArgumentException` if `path` is not a valid actor path.

### `public ActorPath GetChild(string name)`

Creates a new `ActorPath` that is a child of the current path, appending the given segment.  
**Parameters:**  
- `name` – The segment to append (must not be `null` or empty).  
**Returns:** A new `ActorPath` whose `Parent` is the current instance and whose `Name` is `name`.  
**Throws:**  
- `ArgumentNullException` if `name` is `null`.  
- `ArgumentException` if `name` is empty or contains invalid characters.

### `public bool IsDescendantOf(ActorPath other)`

Determines whether the current path is a descendant of the specified ancestor path. A path is considered a descendant if `other` is a strict prefix of the current path’s segments.  
**Parameters:**  
- `other` – The potential ancestor path.  
**Returns:** `true` if the current path is a descendant of `other`; otherwise `false`.  
**Throws:**  
- `ArgumentNullException` if `other` is `null`.

### `public int GetDepth()`

Returns the number of segments in the path. A root path has depth 0.  
**Returns:** The segment count as an integer.

### `public override string ToString()`

Returns the full string representation of the path (same as the `Path` property).  
**Returns:** A string in the canonical path format.

### `public override bool Equals(object? obj)`

Determines whether the current path is equal to another object. Two `ActorPath` instances are equal if their `Path` strings are identical (case‑sensitive).  
**Parameters:**  
- `obj` – The object to compare.  
**Returns:** `true` if `obj` is an `ActorPath` with the same path; otherwise `false`.

### `public bool Equals(ActorPath? other)`

Strongly‑typed equality check.  
**Parameters:**  
- `other` – The other `ActorPath` to compare.  
**Returns:** `true` if the paths are equal; `false` otherwise.

### `public override int GetHashCode()`

Returns a hash code for the path, based on the full string representation.  
**Returns:** A 32‑bit signed integer hash code.

## Usage

### Example 1: Creating paths and navigating the hierarchy

```csharp
using ActorFramework;

// Create a root path
ActorPath root = new ActorPath("/user");
Console.WriteLine(root.Name);       // "user"
Console.WriteLine(root.GetDepth()); // 1

// Create a child path
ActorPath child = root.GetChild("worker");
Console.WriteLine(child.Path);      // "/user/worker"
Console.WriteLine(child.Parent?.Path); // "/user"

// Parse a path from a string
ActorPath parsed = ActorPath.Parse("/system/supervisor/actor");
Console.WriteLine(parsed.Segments[1]); // "supervisor"

// Check ancestry
bool isDesc = child.IsDescendantOf(root);
Console.WriteLine(isDesc); // True
```

### Example 2: Equality and hash codes

```csharp
ActorPath a = new ActorPath("/app/tasks");
ActorPath b = ActorPath.Parse("/app/tasks");
ActorPath c = a.GetChild("task1");

Console.WriteLine(a.Equals(b));   // True (same path)
Console.WriteLine(a == b);        // True if == operator is defined (not shown, but Equals works)
Console.WriteLine(a.GetHashCode() == b.GetHashCode()); // True

// Paths with different segments are not equal
Console.WriteLine(a.Equals(c));   // False
```

## Notes

- **Immutability:** `ActorPath` is immutable. All properties and methods return new instances or read‑only data. This makes the type inherently thread‑safe; no synchronization is required when sharing `ActorPath` instances across threads.
- **Root path:** A root path (e.g., `"/"`) has a `Parent` of `null`, an empty `Segments` list, and a `Name` that may be empty or a reserved root name depending on the implementation. `GetDepth()` returns 0 for a root path.
- **Path format:** The string representation is expected to start with a forward slash (`/`) and use forward slashes as segment separators. Trailing slashes are typically not allowed. The exact validation rules are enforced by the constructor and `Parse` method.
- **Equality:** Equality is based on the full string representation, which is case‑sensitive. Two paths constructed from different strings that normalize to the same canonical form are considered equal only if the implementation performs normalization (this is not guaranteed by the public API alone).
- **Null handling:** All methods that accept a path string or an `ActorPath` argument throw `ArgumentNullException` if the argument is `null`. The `Parent` property may be `null` for root paths.
- **Performance:** `GetChild` and `IsDescendantOf` operate in O(n) time where n is the number of segments. `GetDepth` and property accesses are O(1).
