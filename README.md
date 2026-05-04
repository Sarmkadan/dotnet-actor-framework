// ... (rest of the file remains unchanged)

## ActorMetrics

The `ActorMetrics` class tracks performance and behavior metrics for an actor, providing insights into its message processing and error rates.

### Usage Example

```csharp
var metrics = new ActorMetrics(Guid.NewGuid(), new ActorPath("parent", "child"));
metrics.RecordMessageReceived();
metrics.RecordProcessingTime(100);
metrics.RecordError();

Console.WriteLine($"ActorId: {metrics.ActorId}, ActorPath: {metrics.ActorPath}, MessageCount: {metrics.MessageCount}, ErrorCount: {metrics.ErrorCount}");
Console.WriteLine($"ProcessedCount: {metrics.ProcessedCount}, AverageProcessingTimeMs: {metrics.AverageProcessingTimeMs}, CreatedAt: {metrics.CreatedAt}");
Console.WriteLine($"LastMessageTime: {metrics.LastMessageTime}, MailboxDepth: {metrics.MailboxDepth}");
Console.WriteLine($"ErrorRate: {metrics.GetErrorRate()}, SuccessRate: {metrics.GetSuccessRate()}, Uptime: {metrics.GetUptime()}");
Console.WriteLine($"IsUnhealthy: {metrics.IsUnhealthy()}, Summary: {metrics.GetSummary()}");
```

### Properties and Methods

- `Guid ActorId { get; }`: Gets the unique identifier of the actor.
- `ActorPath ActorPath { get; }`: Gets the path of the referenced actor.
- `long MessageCount { get; private set; }`: Gets the total number of messages processed by the actor.
- `long ErrorCount { get; private set; }`: Gets the total number of errors encountered by the actor.
- `long ProcessedCount { get; private set; }`: Gets the total number of messages processed by the actor.
- `double AverageProcessingTimeMs { get; private set; }`: Gets the average processing time of messages in milliseconds.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when the actor reference was created.
- `DateTime? LastMessageTime { get; private set; }`: Gets the UTC timestamp of the last message received by the actor.
- `int MailboxDepth { get; private set; }`: Gets the current number of messages waiting in the actor's mailbox.
- `void RecordMessageReceived()`: Records that a message was received.
- `void RecordProcessingTime(long elapsedMilliseconds)`: Records the processing time of a message in milliseconds.
- `void RecordError()`: Records that an error occurred processing a message.
- `void UpdateMailboxDepth(int depth)`: Updates the current mailbox depth snapshot.
- `double GetErrorRate()`: Gets the error rate as a percentage.
- `double GetSuccessRate()`: Gets the success rate as a percentage.
- `TimeSpan GetUptime()`: Gets the total uptime since creation.
- `bool IsUnhealthy(double errorRateThreshold = 0.25)`: Checks if the actor is experiencing high error rates.
- `ActorMetricsSummary GetSummary()`: Gets a summary of the metrics.

## ActorSystem

The `ActorSystem` class serves as the root coordinator for the actor framework, managing actor lifecycle, message routing, and system health monitoring. It maintains a registry of all actors and provides methods for creating, querying, and terminating actors within the hierarchy.


### Usage Example

```csharp
// Initialize the actor system
var actorSystem = new ActorSystem("MyActorSystem");

// Create actors in the hierarchy
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("root", ""));
var userActor = await actorSystem.CreateActorAsync(new ActorPath("users", "user1"), rootActor);
var orderActor = await actorSystem.CreateActorAsync(new ActorPath("orders", "order123"), rootActor);

// Get actor references by path
var retrievedUser = actorSystem.GetActorRef(new ActorPath("users", "user1"));

// Get all actors under a specific parent
var userActors = actorSystem.GetActorsByParent(new ActorPath("users", ""));

// Get system-wide metrics
var healthSummary = actorSystem.GetHealthSummary();
Console.WriteLine($"System Health: {healthSummary.GetHealthPercentage()}%");
Console.WriteLine($"Total Messages: {healthSummary.TotalMessages}, Total Errors: {healthSummary.TotalErrors}");

// Terminate an actor
if (retrievedUser != null)
{
    await actorSystem.TerminateActorAsync(retrievedUser);
}

// Shutdown the system
await actorSystem.ShutdownAsync();
```

### Properties

- `string Name { get; }`: Gets the unique name of the actor system.
- `Guid Id { get; }`: Gets the unique identifier of the actor system instance.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when the system was initialized.
- `DateTime? ShutdownAt { get; }`: Gets the UTC timestamp when the system was shut down, if applicable.
- `bool IsRunning { get; }`: Gets a value indicating whether the actor system is currently running and accepting messages.

### Methods

- `ActorSystem(string name)`: Initializes a new instance of the `ActorSystem` class.
- `Task<ActorRef> CreateActorAsync(ActorPath path, ActorRef? supervisor = null)`: Creates and registers a new actor within the system.
- `ActorRef? GetActorRef(ActorPath path)`: Gets an actor reference by its path.
- `IReadOnlyList<ActorRef> GetActorsByParent(ActorPath parentPath)`: Gets all actor references for a given parent path.
- `IReadOnlyList<ActorRef> GetAllActors()`: Gets all registered actors.
- `Task TerminateActorAsync(ActorRef actorRef)`: Terminates an actor by its reference.
- `int GetActorCount()`: Gets the total number of actors in the system.
- `IReadOnlyList<ActorRef> GetErrorActors()`: Gets actors that are in an error state.
- `ActorMetricsSummary? GetActorMetricsSummary(ActorPath path)`: Gets the metrics summary for a specific actor.
- `SystemHealthSummary GetHealthSummary()`: Gets a health summary of all actors.
- `Task ShutdownAsync()`: Gracefully shuts down the actor system.
- `override string ToString()`: Returns a string representation of the actor system.

### Nested Types

- `SystemHealthSummary`: Summary of the actor system health with properties like `SystemId`, `SystemName`, `TotalActors`, `HealthyActors`, `UnhealthyActors`, `ErrorActors`, `TotalMessages`, `TotalErrors`, and methods like `GetHealthPercentage()`, `GetErrorRate()`, and `IsHealthy`.

## Actor

The `Actor` class is the fundamental building block of the actor system. Each actor is a lightweight, single-threaded entity that processes messages sequentially from its mailbox. Actors encapsulate their own state and communicate exclusively through asynchronous message passing, making them ideal for building concurrent, distributed systems with built-in fault tolerance.



### Usage Example

```csharp
// Create a custom actor by inheriting from Actor
public class CounterActor : Actor
{
    public CounterActor(ActorPath path, ActorRef? supervisor = null) 
        : base(path, supervisor)
    {
        // Initialize state
        SetState("count", 0);
    }

    protected override async Task OnInitializeAsync()
    {
        Console.WriteLine($"Counter actor initialized at {Path}");
        await base.OnInitializeAsync();
    }

    protected override async Task OnReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "increment")
        {
            var currentCount = (int)(GetState("count") ?? 0);
            SetState("count", currentCount + 1);
            Console.WriteLine($"Count incremented to {currentCount + 1}");
        }
        else if (message is ControlMessage cm2 && cm2.Command == "get")
        {
            var currentCount = (int)(GetState("count") ?? 0);
            Console.WriteLine($"Current count: {currentCount}");
        }
        await base.OnReceiveAsync(message);
    }

    protected override async Task OnStopAsync()
    {
        Console.WriteLine($"Counter actor stopping at {Path}");
        await base.OnStopAsync();
    }
}

// Usage in an actor system
var actorSystem = new ActorSystem("MySystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));

var counterPath = new ActorPath("/root/counter");
var counterActor = new CounterActor(counterPath, rootActor.Ref);
await counterActor.InitializeAsync();

// Send messages to the actor
await counterActor.ProcessMessageAsync(new ControlMessage("increment", null));
await counterActor.ProcessMessageAsync(new ControlMessage("increment", null));
await counterActor.ProcessMessageAsync(new ControlMessage("get", null));

// Get actor information
Console.WriteLine($"Actor ID: {counterActor.Id}");
Console.WriteLine($"Actor Path: {counterActor.Path}");
Console.WriteLine($"Actor State: {counterActor.State}");
Console.WriteLine($"Created At: {counterActor.CreatedAt}");
Console.WriteLine($"Metrics Summary: {counterActor.GetMetricsSummary()}");

// Terminate the actor
await counterActor.TerminateAsync();
```

### Properties and Methods

- `Guid Id { get; }`: Gets the unique identifier assigned at construction time.
- `ActorRef Ref { get; }`: Gets a serializable reference that other actors use to send messages to this actor.
- `ActorPath Path { get; }`: Gets the hierarchical address within the actor system (e.g., "/user/orders/processor").
- `ActorState State { get; }`: Gets the current lifecycle state of the actor.
- `ActorMetrics Metrics { get; }`: Gets performance counters tracking messages processed, errors, and latency.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when this actor instance was created.
- `DateTime? TerminatedAt { get; }`: Gets the UTC timestamp when the actor was terminated, or null if still alive.
- `ActorRef? Supervisor { get; set; }`: Gets or sets the reference to the supervising actor that handles failures.
- `Task InitializeAsync()`: Transitions the actor from Created to Started state by invoking `OnInitializeAsync`.
- `Task ProcessMessageAsync(Message message)`: Processes a single message, recording metrics and handling errors.
- `void SetState(string key, object value)`: Stores a value in the actor's thread-safe internal state dictionary.
- `object? GetState(string key)`: Retrieves a value from the actor's internal state dictionary.
- `bool HasState(string key)`: Checks if a state key exists.
- `Task TerminateAsync()`: Gracefully terminates the actor by invoking `OnStopAsync` and marking the actor as terminated.
- `ActorMetricsSummary GetMetricsSummary()`: Gets the current metrics summary.
- `override string ToString()`: Returns a string representation of the actor.

## ActorPath

The `ActorPath` class represents the hierarchical path to an actor in the actor system. Paths are immutable and uniquely identify an actor within a system, enabling parent-child relationships and hierarchical navigation.





### Usage Example


```csharp
// Create a root actor path
var rootPath = new ActorPath("/root");
Console.WriteLine(rootPath.Path); // Output: /root
Console.WriteLine(rootPath.Name); // Output: root
Console.WriteLine(rootPath.GetDepth()); // Output: 1

// Create a child actor path
var userPath = rootPath.GetChild("users");
Console.WriteLine(userPath.Path); // Output: /root/users
Console.WriteLine(userPath.Name); // Output: users
Console.WriteLine(userPath.GetDepth()); // Output: 2

// Access parent-child relationships
Console.WriteLine(userPath.Parent?.Path); // Output: /root
Console.WriteLine(userPath.Parent?.Name); // Output: root

// Parse a path from string
var parsedPath = ActorPath.Parse("/root/users/user1");
Console.WriteLine(parsedPath.Path); // Output: /root/users/user1

// Check hierarchical relationships
var orderPath = rootPath.GetChild("orders").GetChild("order123");
Console.WriteLine(orderPath.IsDescendantOf(rootPath)); // Output: True
Console.WriteLine(orderPath.IsDescendantOf(userPath)); // Output: False

// Compare paths
var path1 = new ActorPath("/root/users/user1");
var path2 = new ActorPath("/root/users/user1");
var path3 = new ActorPath("/root/users/user2");
Console.WriteLine(path1.Equals(path2)); // Output: True
Console.WriteLine(path1 == path2); // Output: True
Console.WriteLine(path1.Equals(path3)); // Output: False
```

### Properties and Methods

- `string Path { get; }`: Gets the full path string (e.g., "/root/users/user1").
- `string Name { get; }`: Gets the name of this path segment (e.g., "user1" for "/root/users/user1").
- `ActorPath? Parent { get; }`: Gets the parent path, or null if this is a root path.
- `IReadOnlyList<string> Segments { get; }`: Gets the path segments as a read-only list.
- `ActorPath(string path)`: Initializes a new instance of the `ActorPath` class.
- `static ActorPath Parse(string path)`: Parses a string into an `ActorPath` instance.
- `ActorPath GetChild(string childName)`: Creates a child path from this path.
- `bool IsDescendantOf(ActorPath other)`: Checks if this path is a descendant of another path.
- `int GetDepth()`: Gets the depth of this path in the hierarchy.
- `override string ToString()`: Returns the path string.
- `override bool Equals(object? obj)`: Override of Object.Equals.
- `bool Equals(ActorPath? other)`: Compares this path with another path.
- `override int GetHashCode()`: Override of Object.GetHashCode.

## Message

The `Message` class is the base abstraction for all communication within the actor system, ensuring type safety and traceability for inter-actor interactions. It provides built-in metadata such as a `MessageId`, `CreatedAt` timestamp, and `Priority` for advanced scheduling.

### Usage Example

```csharp
// Creating a command message
var command = new ControlMessage("processData", new Dictionary<string, object> 
{ 
    { "dataId", "123" } 
});

// Accessing message properties
Console.WriteLine($"Message: {command.Command}, Id: {command.MessageId}, Priority: {command.Priority}");

// Handling a response
var response = new ResponseMessage(response: "Success", isSuccess: true);
if (response.IsSuccess)
{
    Console.WriteLine($"Response received: {response.Response}");
}
```
