# ActorPathTestsExtensions

The `ActorPathTestsExtensions` class provides a set of static extension methods and helper utilities designed to facilitate the creation and verification of `ActorPath` instances within unit tests. It enables developers to construct complex path hierarchies, derive sibling or child paths programmatically, and assert structural relationships between paths, such as parentage, sibling status, and segment composition, ensuring robust validation of actor addressing logic.

## API

### `CreateDeepHierarchy`
```csharp
public static ActorPath CreateDeepHierarchy(...)
```
Constructs an `ActorPath` representing a deeply nested hierarchy, typically used for stress testing path resolution or traversal logic.
*   **Parameters**: Accepts variable arguments or configuration specific to the depth and naming convention of the hierarchy (implementation dependent).
*   **Return Value**: Returns a new `ActorPath` instance with multiple nested segments.
*   **Exceptions**: May throw an exception if the requested depth is invalid or if memory constraints prevent hierarchy creation.

### `ShouldHaveSegments`
```csharp
public static void ShouldHaveSegments(this ActorPath path, ...)
```
Asserts that the specified `ActorPath` contains a specific sequence or count of segments.
*   **Parameters**: The target `ActorPath` instance and the expected segments (either as an array of strings or a count).
*   **Return Value**: Void.
*   **Exceptions**: Throws an assertion failure exception if the path's segments do not match the expected criteria.

### `ShouldBeDirectChildOf`
```csharp
public static void ShouldBeDirectChildOf(this ActorPath child, ActorPath parent)
```
Verifies that the `child` path is an immediate descendant of the `parent` path with no intermediate nodes.
*   **Parameters**: The `child` `ActorPath` instance and the expected `parent` `ActorPath` instance.
*   **Return Value**: Void.
*   **Exceptions**: Throws an assertion failure exception if the child is not a direct descendant or if the parent relationship is invalid.

### `ShouldHaveParent`
```csharp
public static void ShouldHaveParent(this ActorPath path, ...)
```
Asserts that the given `ActorPath` has a valid parent path, optionally checking against a specific expected parent.
*   **Parameters**: The target `ActorPath` instance and optionally the expected parent `ActorPath`.
*   **Return Value**: Void.
*   **Exceptions**: Throws an assertion failure exception if the path is a root path (no parent) or if the parent does not match the expected value.

### `CreateSibling`
```csharp
public static ActorPath CreateSibling(this ActorPath path, string siblingName)
```
Generates a new `ActorPath` that shares the same parent as the source path but possesses a different name.
*   **Parameters**: The source `ActorPath` instance and the `string` name for the new sibling.
*   **Return Value**: Returns a new `ActorPath` instance representing the sibling.
*   **Exceptions**: May throw an exception if the source path is a root path (which cannot have siblings in this context) or if the `siblingName` is invalid.

### `GetRelativePath`
```csharp
public static string[]? GetRelativePath(this ActorPath source, ActorPath target)
```
Calculates the relative path segments required to navigate from the `source` path to the `target` path.
*   **Parameters**: The `source` `ActorPath` and the `target` `ActorPath`.
*   **Return Value**: Returns an array of strings representing the relative segments, or `null` if no relative path exists (e.g., disjoint hierarchies).
*   **Exceptions**: Generally does not throw; returns `null` for invalid relationships unless arguments are null.

### `ShouldBeSiblings`
```csharp
public static void ShouldBeSiblings(this ActorPath pathA, ActorPath pathB)
```
Asserts that two `ActorPath` instances share the exact same parent path.
*   **Parameters**: The first `ActorPath` (`pathA`) and the second `ActorPath` (`pathB`).
*   **Return Value**: Void.
*   **Exceptions**: Throws an assertion failure exception if the parents of the two paths differ or if either path lacks a parent.

## Usage

### Example 1: Verifying Hierarchy and Parentage
This example demonstrates creating a path and asserting its relationship to a parent and its segment structure.

```csharp
using ActorFramework;
using ActorFramework.Testing;

// Assume a root path exists
var root = ActorPath.Parse("akka://MySystem");
var parent = root / "department" / "engineering";
var child = parent / "developer-1";

// Assert the child has the correct number of segments
child.ShouldHaveSegments("akka://MySystem", "department", "engineering", "developer-1");

// Verify the direct parent relationship
child.ShouldBeDirectChildOf(parent);

// Verify general parent existence
child.ShouldHaveParent(parent);
```

### Example 2: Creating Siblings and Calculating Relative Paths
This example illustrates generating sibling paths dynamically and computing the navigation path between them.

```csharp
using ActorFramework;
using ActorFramework.Testing;

var baseNode = ActorPath.Parse("akka://System/Cluster/Node1");
var workerA = baseNode / "worker-a";

// Create a sibling path programmatically
var workerB = workerA.CreateSibling("worker-b");

// Assert that both workers are siblings
workerA.ShouldBeSiblings(workerB);

// Calculate the relative path from workerA to workerB
var relative = workerA.GetRelativePath(workerB);

// Expected output: ["..", "worker-b"] or similar depending on implementation specifics
if (relative != null)
{
    Console.WriteLine($"Relative path segments: {string.Join("/", relative)}");
}
```

## Notes

*   **Root Path Edge Cases**: Methods involving parents or siblings (`ShouldBeDirectChildOf`, `CreateSibling`, `ShouldBeSiblings`) will fail or throw exceptions if invoked on root paths, as root paths do not possess a parent context.
*   **Null Handling**: `GetRelativePath` explicitly returns `null` when a relative path cannot be computed (e.g., paths belong to different actor systems), rather than throwing an exception. Callers must handle the nullable return type.
*   **Immutability**: Methods returning `ActorPath` (such as `CreateDeepHierarchy` and `CreateSibling`) return new instances, adhering to the immutability principles typical of path structures; the original instances remain unmodified.
*   **Thread Safety**: As this class consists entirely of static methods operating on immutable `ActorPath` objects and standard assertion logic, it is inherently thread-safe for concurrent use in test suites.
*   **Assertion Dependencies**: The `Should*` methods rely on the underlying test framework's assertion engine. Failures will manifest as standard test assertion exceptions appropriate to the hosting test runner (e.g., xUnit, NUnit).
