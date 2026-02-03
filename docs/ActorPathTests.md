# ActorPathTests

`ActorPathTests` is a unit test class within the `dotnet-actor-framework` project that validates the correctness of the `ActorPath` type and its associated extension methods. It ensures that path construction, hierarchical navigation, equality comparisons, and argument validation behave as specified, covering both valid inputs and edge-case error conditions.

## API

### Constructor_WithValidNestedPath_SetsNameSegmentsAndDepth
- **Purpose**: Verifies that constructing an `ActorPath` from a valid, multi-segment path string correctly populates the internal name segments and depth properties.
- **Parameters**: None (self-contained test method).
- **Return value**: `void`.
- **Throws**: Does not throw; the test fails if the constructor does not set the expected values.

### Constructor_WithNullOrWhitespacePath_ThrowsArgumentException
- **Purpose**: Confirms that passing a `null`, empty, or whitespace-only string to the `ActorPath` constructor immediately raises an `ArgumentException`.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: The test expects the constructor to throw `ArgumentException`; the test itself does not throw.

### Constructor_WithInvalidPathFormat_ThrowsWithDescriptiveMessage
- **Purpose**: Ensures that malformed path strings (e.g., those containing illegal characters or structural violations) cause the constructor to throw an exception whose message contains a human-readable description of the problem.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: The test expects the constructor to throw; the test method does not throw.

### GetChild_WithValidChildName_BuildsCorrectHierarchy
- **Purpose**: Tests the `GetChild` method by appending a valid child name to an existing path and asserting that the resulting `ActorPath` reflects the correct hierarchical relationship and segment sequence.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw; the test fails if the child path is incorrect.

### IsDescendantOf_WhenPathNested_ReturnsTrue_AndSiblingReturnsFalse
- **Purpose**: Validates the `IsDescendantOf` logic by checking that a deeply nested path is correctly identified as a descendant of an ancestor path, while a sibling path (same parent, different leaf) is not considered a descendant.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw.

### Equality_WithIdenticalPathStrings_PathsAreEqual
- **Purpose**: Confirms that two `ActorPath` instances created from identical path strings are considered equal according to the type’s equality implementation (both `Equals` and `==`).
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw.

### IsChildOf_ExtensionMethod_ReturnsTrueForAnyDescendant
- **Purpose**: Exercises the `IsChildOf` extension method to ensure it returns `true` for any descendant path, not just immediate children, matching the expected semantic contract.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw.

## Usage

### Example 1: Validating path construction and hierarchy
```csharp
[TestMethod]
public void Constructor_WithValidNestedPath_SetsNameSegmentsAndDepth()
{
    var path = new ActorPath("/root/branch/leaf");
    Assert.AreEqual(3, path.Depth);
    CollectionAssert.AreEqual(new[] { "root", "branch", "leaf" }, path.Segments);
}

[TestMethod]
public void GetChild_WithValidChildName_BuildsCorrectHierarchy()
{
    var parent = new ActorPath("/root/branch");
    var child = parent.GetChild("leaf");
    Assert.AreEqual("/root/branch/leaf", child.ToString());
    Assert.IsTrue(child.IsDescendantOf(parent));
}
```

### Example 2: Testing error conditions and equality
```csharp
[TestMethod]
public void Constructor_WithNullOrWhitespacePath_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new ActorPath(null));
    Assert.ThrowsException<ArgumentException>(() => new ActorPath("   "));
}

[TestMethod]
public void Equality_WithIdenticalPathStrings_PathsAreEqual()
{
    var path1 = new ActorPath("/service/instance");
    var path2 = new ActorPath("/service/instance");
    Assert.AreEqual(path1, path2);
    Assert.IsTrue(path1 == path2);
}
```

## Notes

- **Edge cases**: The tests explicitly cover `null`, empty, and whitespace-only input strings, as well as malformed path formats. Descendant checks distinguish between direct children, deeper descendants, and siblings to prevent false positives.
- **Thread-safety**: These test methods are synchronous and single-threaded by nature (standard unit test execution). They do not validate thread-safety of `ActorPath` itself; concurrent access characteristics must be inferred from the production type’s implementation, not from this test class.
- **Extension method scope**: `IsChildOf` is tested as an extension method, meaning its behavior depends on the static class in which it is defined. The test confirms it operates on any `ActorPath` instance and returns `true` for all descendants, not merely immediate children.
