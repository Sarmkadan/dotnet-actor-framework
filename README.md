// ... (rest of the file remains unchanged)

## LoadBasedRouter

// ... (rest of the file remains unchanged)

## ActorRef

The `ActorRef` provides a thread-safe reference to an actor, enabling message sending and management of actor interactions. It encapsulates essential actor metadata and behavior.

### Usage Example

```csharp
var parent = new ActorRef(new ActorPath("parent"), Guid.NewGuid());
var child = new ActorRef(new ActorPath("parent", "child"), Guid.NewGuid());

Console.WriteLine($"Path: {child.Path}, ID: {child.Id:N}, IsAlive: {child.IsAlive}, CreatedAt: {child.CreatedAt}");

await child.SendAsync(new MyMessage()); // Example message sending

var response = await child.AskAsync(new RequestMessage(), TimeSpan.FromSeconds(5));
Console.WriteLine($"Received response: {response}");

var parentRef = child.GetParent();
Console.WriteLine($"Parent ActorRef: {parentRef?.ToString()}");

Console.WriteLine(child.Equals(parentRef)); // Equality comparison
```

### Properties and Methods

- `ActorPath Path { get; }`: Gets the path of the referenced actor.
- `Guid Id { get; }`: Gets the unique identifier of the referenced actor.
- `bool IsAlive { get; }`: Gets a value indicating whether the actor is alive.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when the actor reference was created.
- `async Task SendAsync(object message)`: Sends a message to the actor asynchronously.
- `async Task<object?> AskAsync(object message, TimeSpan timeout)`: Sends a message and waits for a response within a specified timeout.
- `ActorRef? GetParent()`: Gets the parent actor reference.
- `override string ToString()`: Returns a string representation of the actor reference.
- `override bool Equals(object? obj)`: Compares this actor reference with another object for equality.
- `bool Equals(ActorRef? other)`: Compares this actor reference with another actor reference for equality.
- `override int GetHashCode()`: Returns the hash code for this actor reference.
